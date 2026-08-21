Imports System.Threading.Tasks

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

    ' ===================== Students page =====================

    Public Class StudentDto
        Public Property UserId As Integer
        Public Property DisplayName As String
        Public Property Email As String
        Public Property JoinedText As String
        Public Property LastLoginText As String
        Public Property IsActive As Boolean
    End Class

    Public Async Function GetAllStudentsAsync() As Task(Of List(Of StudentDto))
        Const sql As String = "
            SELECT user_id, display_name, email, created_at, last_login_at, is_active
            FROM users
            WHERE role = 'Student'
            ORDER BY display_name"

        Return Await Db.QueryAsync(Of StudentDto)(
            sql,
            Function(r) New StudentDto With {
                .UserId = r.GetInt32("user_id"),
                .DisplayName = r.GetString("display_name"),
                .Email = r.GetString("email"),
                .JoinedText = r.GetDateTime("created_at").ToString("dd MMM yyyy"),
                .LastLoginText = If(r.IsDBNull(r.GetOrdinal("last_login_at")), "Never", r.GetDateTime("last_login_at").ToString("dd MMM yyyy")),
                .IsActive = r.GetBoolean("is_active")
            })
    End Function

    Public Async Function SetStudentActiveAsync(userId As Integer, isActive As Boolean, actorName As String) As Task
        Await Db.ExecuteAsync(
            "UPDATE users SET is_active = @active WHERE user_id = @id",
            New Dictionary(Of String, Object) From {{"@active", isActive}, {"@id", userId}})
        Await UsersRepository.LogActivityAsync(Nothing, If(isActive, "student_reactivated", "student_deactivated"),
                                                 $"{actorName} {If(isActive, "reactivated", "deactivated")} student account #{userId}")
    End Function

    ' ===================== Teachers page =====================

    Public Class TeacherDto
        Public Property UserId As Integer
        Public Property DisplayName As String
        Public Property Email As String
        Public Property ApprovalStatus As String ' "Approved" / "Pending" / "Denied"
        Public Property JoinedText As String
    End Class

    Public Async Function GetAllTeachersAsync() As Task(Of List(Of TeacherDto))
        Const sql As String = "
            SELECT user_id, display_name, email, approval_status, created_at
            FROM users
            WHERE role = 'Teacher'
            ORDER BY FIELD(approval_status, 'Pending', 'Approved', 'Denied'), display_name"

        Return Await Db.QueryAsync(Of TeacherDto)(
            sql,
            Function(r) New TeacherDto With {
                .UserId = r.GetInt32("user_id"),
                .DisplayName = r.GetString("display_name"),
                .Email = r.GetString("email"),
                .ApprovalStatus = r.GetString("approval_status"),
                .JoinedText = r.GetDateTime("created_at").ToString("dd MMM yyyy")
            })
    End Function

    ' ===================== Reports page =====================

    Public Class PlatformStatsDto
        Public Property TotalStudents As Integer
        Public Property TotalTeachers As Integer
        Public Property PendingTeachers As Integer
        Public Property TotalQuizAttempts As Integer
        Public Property AverageQuizScore As Decimal
        Public Property TotalAssessments As Integer
        Public Property AverageAssessmentScore As Decimal
    End Class

    Public Async Function GetPlatformStatsAsync() As Task(Of PlatformStatsDto)
        Dim result As New PlatformStatsDto()

        result.TotalStudents = CInt(Await Db.ScalarAsync(Of Long)("SELECT COUNT(*) FROM users WHERE role = 'Student'"))
        result.TotalTeachers = CInt(Await Db.ScalarAsync(Of Long)("SELECT COUNT(*) FROM users WHERE role = 'Teacher' AND approval_status = 'Approved'"))
        result.PendingTeachers = CInt(Await Db.ScalarAsync(Of Long)("SELECT COUNT(*) FROM users WHERE role = 'Teacher' AND approval_status = 'Pending'"))
        result.TotalQuizAttempts = CInt(Await Db.ScalarAsync(Of Long)("SELECT COUNT(*) FROM quiz_attempts WHERE submitted_at IS NOT NULL"))

        Dim avgQuiz = Await Db.ScalarAsync(Of Decimal)("SELECT AVG(score_percent) FROM quiz_attempts WHERE submitted_at IS NOT NULL")
        result.AverageQuizScore = avgQuiz

        result.TotalAssessments = CInt(Await Db.ScalarAsync(Of Long)("SELECT COUNT(*) FROM assessments WHERE status = 'Graded'"))
        Dim avgAssessment = Await Db.ScalarAsync(Of Decimal)("SELECT AVG(score_percent) FROM assessments WHERE status = 'Graded'")
        result.AverageAssessmentScore = avgAssessment

        Return result
    End Function

    Public Class TopStudentDto
        Public Property DisplayName As String
        Public Property AverageScore As Decimal
        Public Property AssessmentCount As Integer
    End Class

    Public Async Function GetTopStudentsAsync(Optional limit As Integer = 5) As Task(Of List(Of TopStudentDto))
        Dim sql As String = $"
            SELECT u.display_name, AVG(a.score_percent) AS avg_score, COUNT(*) AS assessment_count
            FROM assessments a
            JOIN users u ON u.user_id = a.user_id
            WHERE a.status = 'Graded'
            GROUP BY u.user_id, u.display_name
            ORDER BY avg_score DESC
            LIMIT {Math.Max(1, limit)}"

        Return Await Db.QueryAsync(Of TopStudentDto)(
            sql,
            Function(r) New TopStudentDto With {
                .DisplayName = r.GetString("display_name"),
                .AverageScore = r.GetDecimal("avg_score"),
                .AssessmentCount = r.GetInt32("assessment_count")
            })
    End Function

End Module
