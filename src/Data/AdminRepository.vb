''' <summary>Backs AdminDashboardForm's "Pending teacher approvals" and "Recent activity" panels.</summary>
Public Module AdminRepository

    Public Class PendingTeacherDto
        Public Property UserId As Integer
        Public Property DisplayLine As String ' "Dr. Sarah Whitfield — sarah@school.test"
    End Class

    Public Async Function GetPendingTeachersAsync() As Task(Of List(Of PendingTeacherDto))
        Const sql As String = "
            SELECT user_id, display_name, email
            FROM users
            WHERE role = 'Teacher' AND approval_status = 'Pending'
            ORDER BY created_at"

        Return Await Db.QueryAsync(Of PendingTeacherDto)(
            sql,
            Function(r) New PendingTeacherDto With {
                .UserId = r.GetInt32("user_id"),
                .DisplayLine = $"{r.GetString("display_name")} — {r.GetString("email")}"
            })
    End Function

    Public Async Function ApproveTeacherAsync(userId As Integer, approvedByName As String) As Task
        Await Db.ExecuteAsync(
            "UPDATE users SET approval_status = 'Approved' WHERE user_id = @id",
            New Dictionary(Of String, Object) From {{"@id", userId}})
        Await UsersRepository.LogActivityAsync(Nothing, "teacher_approved", $"{approvedByName} approved teacher account #{userId}")
    End Function

    Public Async Function DenyTeacherAsync(userId As Integer, deniedByName As String) As Task
        Await Db.ExecuteAsync(
            "UPDATE users SET approval_status = 'Denied' WHERE user_id = @id",
            New Dictionary(Of String, Object) From {{"@id", userId}})
        Await UsersRepository.LogActivityAsync(Nothing, "teacher_denied", $"{deniedByName} denied teacher account #{userId}")
    End Function

    Public Class ActivityDto
        Public Property Who As String
        Public Property What As String
        Public Property WhenText As String ' e.g. "2 min ago"
    End Class

    Public Async Function GetRecentActivityAsync(Optional limit As Integer = 5) As Task(Of List(Of ActivityDto))
        ' MySqlConnector doesn't parameterize LIMIT well on every version, and
        ' `limit` here is a hard-coded caller value (never user input), so
        ' string-formatting it in is fine.
        Dim sql As String = $"
            SELECT COALESCE(u.display_name, 'System') AS who_name, a.description, a.created_at
            FROM activity_log a
            LEFT JOIN users u ON u.user_id = a.user_id
            ORDER BY a.created_at DESC
            LIMIT {Math.Max(1, limit)}"

        Dim rows = Await Db.QueryAsync(Of ActivityDto)(
            sql,
            Function(r) New ActivityDto With {
                .Who = r.GetString("who_name"),
                .What = r.GetString("description"),
                .WhenText = FormatRelativeTime(r.GetDateTime("created_at"))
            })
        Return rows
    End Function

    Private Function FormatRelativeTime(timestamp As DateTime) As String
        Dim delta = DateTime.Now - timestamp
        If delta.TotalMinutes < 1 Then Return "just now"
        If delta.TotalMinutes < 60 Then Return $"{CInt(delta.TotalMinutes)} min ago"
        If delta.TotalHours < 24 Then Return $"{CInt(delta.TotalHours)} hr ago"
        Return $"{CInt(delta.TotalDays)} d ago"
    End Function

End Module
