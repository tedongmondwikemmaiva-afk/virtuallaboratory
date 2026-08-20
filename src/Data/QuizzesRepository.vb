Imports System.Linq
Imports System.Threading.Tasks
Imports MySqlConnector

''' <summary>
''' Loads a quiz (with its questions and options) for the Quizzes screen, and
''' saves completed attempts back to the database.
''' </summary>
Public Module QuizzesRepository

    Public Class QuizWithQuestions
        Public Property Subject As String
        ' Each Quizzes.QuizQuestion here has QuestionId/OptionIds populated,
        ' unlike the form's offline fallback list (which uses 0 for both).
        Public Property Questions As List(Of Quizzes.QuizQuestion)
    End Class

    Public Async Function GetQuizWithQuestionsAsync(quizId As Integer) As Task(Of QuizWithQuestions)
        Dim subjectRows = Await Db.QueryAsync(Of String)(
            "SELECT subject FROM quizzes WHERE quiz_id = @id LIMIT 1",
            Function(r) r.GetString("subject"),
            New Dictionary(Of String, Object) From {{"@id", quizId}})

        Dim result As New QuizWithQuestions With {
            .Subject = If(subjectRows.Count > 0, subjectRows(0), "General"),
            .Questions = New List(Of Quizzes.QuizQuestion)
        }
        If subjectRows.Count = 0 Then Return result ' quiz doesn't exist — caller keeps its offline fallback

        Dim questionRows = Await Db.QueryAsync(Of (Id As Integer, Text As String))(
            "SELECT question_id, question_text FROM quiz_questions WHERE quiz_id = @id ORDER BY sort_order",
            Function(r) (r.GetInt32("question_id"), r.GetString("question_text")),
            New Dictionary(Of String, Object) From {{"@id", quizId}})

        For Each qr In questionRows
            Dim optionRows = Await Db.QueryAsync(Of (Id As Integer, Text As String, Correct As Boolean))(
                "SELECT option_id, option_text, is_correct FROM quiz_options WHERE question_id = @qid ORDER BY sort_order",
                Function(r) (r.GetInt32("option_id"), r.GetString("option_text"), r.GetBoolean("is_correct")),
                New Dictionary(Of String, Object) From {{"@qid", qr.Id}})

            Dim correctIdx = optionRows.FindIndex(Function(o) o.Correct)
            result.Questions.Add(New Quizzes.QuizQuestion With {
                .QuestionId = qr.Id,
                .Text = qr.Text,
                .Options = optionRows.Select(Function(o) o.Text).ToArray(),
                .OptionIds = optionRows.Select(Function(o) o.Id).ToArray(),
                .CorrectIndex = Math.Max(correctIdx, 0),
                .Selected = -1
            })
        Next

        Return result
    End Function

    ''' <summary>
    ''' Saves a completed attempt (header row + one row per answered question)
    ''' as a single transaction, and rolls the result into mastery_topics so
    ''' Reports & Grades picks it up too.
    ''' </summary>
    Public Async Function SaveAttemptAsync(quizId As Integer, userId As Integer, scorePercent As Integer,
                                            questions As List(Of Quizzes.QuizQuestion)) As Task
        Await Db.RunInTransactionAsync(
            Async Function(conn, tx)
                Using cmd As New MySqlConnector.MySqlCommand(
                    "INSERT INTO quiz_attempts (quiz_id, user_id, submitted_at, score_percent)
                     VALUES (@quiz, @user, NOW(), @score)", conn, tx)
                    cmd.Parameters.AddWithValue("@quiz", quizId)
                    cmd.Parameters.AddWithValue("@user", userId)
                    cmd.Parameters.AddWithValue("@score", scorePercent)
                    Await cmd.ExecuteNonQueryAsync()
                End Using

                Dim attemptId As Long
                Using idCmd As New MySqlConnector.MySqlCommand("SELECT LAST_INSERT_ID();", conn, tx)
                    attemptId = CLng(Await idCmd.ExecuteScalarAsync())
                End Using

                For Each q In questions
                    If q.QuestionId <= 0 OrElse q.Selected < 0 Then Continue For ' unanswered, or offline fallback question
                    Dim selectedOptionId = If(q.OptionIds IsNot Nothing AndAlso q.Selected < q.OptionIds.Length,
                                               CType(q.OptionIds(q.Selected), Object), DBNull.Value)
                    Using cmd As New MySqlConnector.MySqlCommand(
                        "INSERT INTO quiz_attempt_answers (attempt_id, question_id, selected_option_id, is_correct)
                         VALUES (@attempt, @question, @option, @correct)", conn, tx)
                        cmd.Parameters.AddWithValue("@attempt", attemptId)
                        cmd.Parameters.AddWithValue("@question", q.QuestionId)
                        cmd.Parameters.AddWithValue("@option", selectedOptionId)
                        cmd.Parameters.AddWithValue("@correct", q.Selected = q.CorrectIndex)
                        Await cmd.ExecuteNonQueryAsync()
                    End Using
                Next
            End Function)
    End Function

End Module
