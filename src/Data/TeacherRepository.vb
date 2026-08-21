Imports System.Threading.Tasks

''' <summary>Backs TeacherDashboardForm's stat cards, student roster, class-average chart, and grading queue.</summary>
Public Module TeacherRepository

    Public Class TeacherStatsDto
        Public Property StudentsEnrolled As Integer
        Public Property LiveInLabNow As Integer
        Public Property AwaitingGrading As Integer
        Public Property ClassAverage As Decimal
    End Class

    Public Async Function GetStatsAsync() As Task(Of TeacherStatsDto)
        Dim result As New TeacherStatsDto()
        result.StudentsEnrolled = CInt(Await Db.ScalarAsync(Of Long)("SELECT COUNT(*) FROM users WHERE role = 'Student'"))
        result.LiveInLabNow = CInt(Await Db.ScalarAsync(Of Long)(
            "SELECT COUNT(DISTINCT user_id) FROM lab_sessions WHERE ended_at IS NULL"))
        result.AwaitingGrading = CInt(Await Db.ScalarAsync(Of Long)(
            "SELECT COUNT(*) FROM assessments WHERE status = 'Pending'"))
        result.ClassAverage = Await Db.ScalarAsync(Of Decimal)(
            "SELECT AVG(score_percent) FROM assessments WHERE status = 'Graded'")
        Return result
    End Function

    ''' <summary>
    ''' One row per student: name, class (or "Unassigned"), how many graded
    ''' assessments they have, their average score, and whether they currently
    ''' have an open lab_sessions row (no ended_at yet = "In lab").
    ''' </summary>
    Public Async Function GetStudentsOverviewAsync() As Task(Of List(Of (Name As String, ClassName As String, Completed As String, Average As String, Status As String)))
        Const sql As String = "
            SELECT
                u.display_name,
                COALESCE(u.class_name, 'Unassigned') AS class_name,
                COUNT(a.assessment_id) AS completed_count,
                AVG(a.score_percent) AS avg_score,
                EXISTS(SELECT 1 FROM lab_sessions ls WHERE ls.user_id = u.user_id AND ls.ended_at IS NULL) AS in_lab
            FROM users u
            LEFT JOIN assessments a ON a.user_id = u.user_id AND a.status = 'Graded'
            WHERE u.role = 'Student'
            GROUP BY u.user_id, u.display_name, u.class_name
            ORDER BY u.display_name"

        Return Await Db.QueryAsync(Of (String, String, String, String, String))(
            sql,
            Function(r)
                Dim completed = r.GetInt32("completed_count")
                Dim avgScoreText = If(r.IsDBNull(r.GetOrdinal("avg_score")), "—", $"{Math.Round(r.GetDecimal("avg_score"), 0)}%")
                Dim status = If(r.GetBoolean("in_lab"), "In lab", "Offline")
                Return (r.GetString("display_name"), r.GetString("class_name"), completed.ToString(), avgScoreText, status)
            End Function)
    End Function

    ''' <summary>Average graded-assessment score per assigned class. Students with no class_name are excluded (they have nothing to group into).</summary>
    Public Async Function GetClassAveragesAsync() As Task(Of List(Of (ClassName As String, Average As Integer)))
        Const sql As String = "
            SELECT u.class_name, AVG(a.score_percent) AS avg_score
            FROM users u
            JOIN assessments a ON a.user_id = u.user_id AND a.status = 'Graded'
            WHERE u.role = 'Student' AND u.class_name IS NOT NULL
            GROUP BY u.class_name
            ORDER BY u.class_name"

        Return Await Db.QueryAsync(Of (String, Integer))(
            sql,
            Function(r) (r.GetString("class_name"), CInt(Math.Round(r.GetDecimal("avg_score"), 0))))
    End Function

    ''' <summary>Ungraded assessments, oldest first — what a teacher needs to work through.</summary>
    Public Async Function GetGradingQueueAsync() As Task(Of List(Of (AssessmentId As Integer, Title As String, StudentName As String)))
        Const sql As String = "
            SELECT a.assessment_id, a.name, u.display_name
            FROM assessments a
            JOIN users u ON u.user_id = a.user_id
            WHERE a.status = 'Pending'
            ORDER BY a.assessed_on"

        Return Await Db.QueryAsync(Of (Integer, String, String))(
            sql,
            Function(r) (r.GetInt32("assessment_id"), r.GetString("name"), r.GetString("display_name")))
    End Function

    Public Async Function GradeAssessmentAsync(assessmentId As Integer, scorePercent As Integer, gradedByName As String) As Task
        Await Db.ExecuteAsync(
            "UPDATE assessments SET score_percent = @score, status = 'Graded' WHERE assessment_id = @id",
            New Dictionary(Of String, Object) From {{"@score", scorePercent}, {"@id", assessmentId}})
        Await UsersRepository.LogActivityAsync(Nothing, "assessment_graded", $"{gradedByName} graded assessment #{assessmentId} ({scorePercent}%)")
    End Function

    Public Async Function SetStudentClassAsync(userId As Integer, className As String, actorName As String) As Task
        Dim value As Object = If(String.IsNullOrWhiteSpace(className), CType(DBNull.Value, Object), className.Trim())
        Await Db.ExecuteAsync(
            "UPDATE users SET class_name = @class WHERE user_id = @id",
            New Dictionary(Of String, Object) From {{"@class", value}, {"@id", userId}})
        Await UsersRepository.LogActivityAsync(Nothing, "student_class_set", $"{actorName} set a student's class to '{className}'")
    End Function

    ''' <summary>Resolves a student's user_id from their display name, for the "set class" action (same stand-in pattern as UsersRepository.FindUserIdByDisplayNameAsync).</summary>
    Public Async Function FindStudentIdByNameAsync(displayName As String) As Task(Of Integer?)
        Dim id = Await Db.ScalarAsync(Of Long)(
            "SELECT user_id FROM users WHERE display_name = @name AND role = 'Student' LIMIT 1",
            New Dictionary(Of String, Object) From {{"@name", displayName}})
        If id = 0 Then Return Nothing
        Return CInt(id)
    End Function

End Module
