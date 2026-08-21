' Deliberately not "Imports BCrypt.Net" — VB was resolving the unqualified
' "BCrypt" to the namespace segment rather than the BCrypt class inside it
' (BC30456: 'Verify'/'HashPassword' is not a member of 'BCrypt'). Fully
' qualifying with Global.BCrypt.Net.BCrypt below sidesteps that ambiguity.

''' <summary>
''' Auth-related queries. Passwords are always hashed with BCrypt — never
''' compare or store plaintext passwords anywhere in this class.
''' Requires the "BCrypt.Net-Next" NuGet package.
''' </summary>
Public Module UsersRepository

    Public Class UserRecord
        Public Property UserId As Integer
        Public Property DisplayName As String
        Public Property Email As String
        Public Property Role As String
        Public Property ApprovalStatus As String
    End Class

    ''' <summary>
    ''' Verifies an email/password pair. Returns Nothing if the user doesn't
    ''' exist, the password is wrong, the account is inactive, or (for
    ''' Teachers) approval is still Pending/Denied.
    ''' </summary>
    Public Async Function AuthenticateAsync(email As String, plainPassword As String) As Task(Of UserRecord)
        Const sql As String = "
            SELECT user_id, display_name, email, password_hash, role, approval_status
            FROM users
            WHERE email = @email AND is_active = 1
            LIMIT 1"

        Dim rows = Await Db.QueryAsync(Of (User As UserRecord, Hash As String))(
            sql,
            Function(r) (
                New UserRecord With {
                    .UserId = r.GetInt32("user_id"),
                    .DisplayName = r.GetString("display_name"),
                    .Email = r.GetString("email"),
                    .Role = r.GetString("role"),
                    .ApprovalStatus = r.GetString("approval_status")
                },
                r.GetString("password_hash")
            ),
            New Dictionary(Of String, Object) From {{"@email", email}})

        If rows.Count = 0 Then Return Nothing

        Dim user = rows(0).User
        Dim storedHash = rows(0).Hash

        If Not Global.BCrypt.Net.BCrypt.Verify(plainPassword, storedHash) Then Return Nothing
        If user.Role = "Teacher" AndAlso user.ApprovalStatus <> "Approved" Then Return Nothing

        Await Db.ExecuteAsync("UPDATE users SET last_login_at = NOW() WHERE user_id = @id",
                               New Dictionary(Of String, Object) From {{"@id", user.UserId}})
        Await LogActivityAsync(user.UserId, "login", $"{user.DisplayName} signed in")

        Return user
    End Function

    ''' <summary>
    ''' Creates a new account. Students are always auto-approved. Whether Teachers
    ''' need Admin approval is governed by the "require_teacher_approval" system
    ''' setting — if an Admin turns that off, new Teacher signups are approved
    ''' immediately instead of landing in the Pending queue.
    ''' </summary>
    Public Async Function CreateAccountAsync(displayName As String, email As String, plainPassword As String, role As String) As Task(Of Long)
        Dim hash = Global.BCrypt.Net.BCrypt.HashPassword(plainPassword) ' work factor defaults to 11, that's fine

        Dim approvalStatus As String
        If role = "Teacher" Then
            Dim requireApproval = Await SettingsRepository.GetBoolAsync("require_teacher_approval", True)
            approvalStatus = If(requireApproval, "Pending", "Approved")
        Else
            approvalStatus = "Approved"
        End If

        Const sql As String = "
            INSERT INTO users (display_name, email, password_hash, role, approval_status)
            VALUES (@name, @email, @hash, @role, @approval)"

        Dim newId = Await Db.ExecuteInsertAsync(sql, New Dictionary(Of String, Object) From {
            {"@name", displayName}, {"@email", email}, {"@hash", hash},
            {"@role", role}, {"@approval", approvalStatus}
        })
        Await LogActivityAsync(CInt(newId), "account_created", $"{displayName} created a {role} account")
        Return newId
    End Function

    Public Async Function EmailExistsAsync(email As String) As Task(Of Boolean)
        Dim count = Await Db.ScalarAsync(Of Long)(
            "SELECT COUNT(*) FROM users WHERE email = @email",
            New Dictionary(Of String, Object) From {{"@email", email}})
        Return count > 0
    End Function

    ''' <summary>
    ''' Best-effort lookup of a user's id from their display name, used by
    ''' screens that only have LoginForm's SignedInName to go on (no proper
    ''' session/user-id plumbing exists yet). Matches on display_name, so it's
    ''' ambiguous if two accounts share a name — replace with a real session
    ''' object carrying the actual user_id once you have one.
    ''' </summary>
    Public Async Function FindUserIdByDisplayNameAsync(displayName As String) As Task(Of Integer?)
        If String.IsNullOrWhiteSpace(displayName) Then Return Nothing
        Dim id = Await Db.ScalarAsync(Of Long)(
            "SELECT user_id FROM users WHERE display_name = @name LIMIT 1",
            New Dictionary(Of String, Object) From {{"@name", displayName}})
        If id = 0 Then Return Nothing
        Return CInt(id)
    End Function

    Public Async Function LogActivityAsync(userId As Integer?, eventType As String, description As String) As Task
        Await Db.ExecuteAsync(
            "INSERT INTO activity_log (user_id, event_type, description) VALUES (@uid, @type, @desc)",
            New Dictionary(Of String, Object) From {{"@uid", userId}, {"@type", eventType}, {"@desc", description}})
    End Function

End Module
