''' <summary>
''' Backs the Reports &amp; Grades screen's score trend chart, mastery bar
''' chart, and assessment history table. Also used by the Quizzes screen for
''' its "Your scores" panel (same mastery_topics table).
''' </summary>
Public Module ReportsRepository

    Public Async Function GetScoreTrendAsync(userId As Integer) As Task(Of List(Of (Month As String, Score As Integer)))
        Const sql As String = "
            SELECT DATE_FORMAT(period_month, '%b') AS month_label, score_percent
            FROM score_trend
            WHERE user_id = @uid
            ORDER BY period_month"

        Return Await Db.QueryAsync(Of (String, Integer))(
            sql,
            Function(r) (r.GetString("month_label"), CInt(r.GetDecimal("score_percent"))),
            New Dictionary(Of String, Object) From {{"@uid", userId}})
    End Function

    Public Async Function GetMasteryTopicsAsync(userId As Integer) As Task(Of List(Of (Topic As String, Percent As Integer)))
        Const sql As String = "
            SELECT topic, mastery_percent
            FROM mastery_topics
            WHERE user_id = @uid
            ORDER BY topic"

        Return Await Db.QueryAsync(Of (String, Integer))(
            sql,
            Function(r) (r.GetString("topic"), CInt(r.GetDecimal("mastery_percent"))),
            New Dictionary(Of String, Object) From {{"@uid", userId}})
    End Function

    Public Class AssessmentDto
        Public Property Name As String
        Public Property Type As String
        Public Property DateText As String
        Public Property Score As String
        Public Property Status As String
    End Class

    Public Async Function GetAssessmentsAsync(userId As Integer) As Task(Of List(Of AssessmentDto))
        Const sql As String = "
            SELECT name, type, DATE_FORMAT(assessed_on, '%d %b %Y') AS date_text, score_percent, status
            FROM assessments
            WHERE user_id = @uid
            ORDER BY assessed_on"

        Return Await Db.QueryAsync(Of AssessmentDto)(
            sql,
            Function(r) New AssessmentDto With {
                .Name = r.GetString("name"),
                .Type = r.GetString("type"),
                .DateText = r.GetString("date_text"),
                .Score = If(r.IsDBNull(r.GetOrdinal("score_percent")), "—", CInt(r.GetDecimal("score_percent")) & "%"),
                .Status = r.GetString("status")
            },
            New Dictionary(Of String, Object) From {{"@uid", userId}})
    End Function

End Module
