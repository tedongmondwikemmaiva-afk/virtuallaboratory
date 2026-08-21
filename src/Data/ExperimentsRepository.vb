Imports System.Linq
Imports System.Threading.Tasks

''' <summary>
''' Backs the admin "Experiments Library" page (create/publish/archive/delete)
''' and the student-facing ExperimentsForm (browse published experiments,
''' track start/completion).
''' </summary>
Public Module ExperimentsRepository

    Public Class ExperimentDto
        Public Property ExperimentId As Integer
        Public Property Title As String
        Public Property Description As String
        Public Property Category As String
        Public Property Difficulty As String
        Public Property EstDurationMinutes As Integer
        Public Property Status As String ' "Draft" / "Published" / "Archived"
        Public Property AuthorName As String
        Public Property CreatedText As String
        Public Property CompletionCount As Integer
    End Class

    ''' <summary>Admin/Teacher view: every experiment regardless of status, with completion counts.</summary>
    Public Async Function GetAllAsync() As Task(Of List(Of ExperimentDto))
        Const sql As String = "
            SELECT e.experiment_id, e.title, e.description, e.category, e.difficulty,
                   e.est_duration_minutes, e.status, e.created_at,
                   COALESCE(u.display_name, 'Unknown') AS author_name,
                   (SELECT COUNT(*) FROM experiment_completions c
                    WHERE c.experiment_id = e.experiment_id AND c.completed_at IS NOT NULL) AS completion_count
            FROM experiments e
            LEFT JOIN users u ON u.user_id = e.created_by
            ORDER BY e.created_at DESC"

        Return Await Db.QueryAsync(Of ExperimentDto)(sql, AddressOf MapExperiment)
    End Function

    ''' <summary>Student view: only Published experiments.</summary>
    Public Async Function GetPublishedAsync() As Task(Of List(Of ExperimentDto))
        Const sql As String = "
            SELECT e.experiment_id, e.title, e.description, e.category, e.difficulty,
                   e.est_duration_minutes, e.status, e.created_at,
                   COALESCE(u.display_name, 'Unknown') AS author_name,
                   (SELECT COUNT(*) FROM experiment_completions c
                    WHERE c.experiment_id = e.experiment_id AND c.completed_at IS NOT NULL) AS completion_count
            FROM experiments e
            LEFT JOIN users u ON u.user_id = e.created_by
            WHERE e.status = 'Published'
            ORDER BY e.category, e.title"

        Return Await Db.QueryAsync(Of ExperimentDto)(sql, AddressOf MapExperiment)
    End Function

    Private Function MapExperiment(r As MySqlConnector.MySqlDataReader) As ExperimentDto
        Return New ExperimentDto With {
            .ExperimentId = r.GetInt32("experiment_id"),
            .Title = r.GetString("title"),
            .Description = r.GetString("description"),
            .Category = r.GetString("category"),
            .Difficulty = r.GetString("difficulty"),
            .EstDurationMinutes = r.GetInt32("est_duration_minutes"),
            .Status = r.GetString("status"),
            .AuthorName = r.GetString("author_name"),
            .CreatedText = r.GetDateTime("created_at").ToString("dd MMM yyyy"),
            .CompletionCount = r.GetInt32("completion_count")
        }
    End Function

    Public Async Function CreateAsync(title As String, description As String, category As String,
                                       difficulty As String, durationMinutes As Integer, createdBy As Integer?) As Task(Of Long)
        Const sql As String = "
            INSERT INTO experiments (title, description, category, difficulty, est_duration_minutes, status, created_by)
            VALUES (@title, @desc, @cat, @diff, @dur, 'Draft', @by)"

        Dim newId = Await Db.ExecuteInsertAsync(sql, New Dictionary(Of String, Object) From {
            {"@title", title}, {"@desc", description}, {"@cat", category},
            {"@diff", difficulty}, {"@dur", durationMinutes}, {"@by", createdBy}
        })
        Await UsersRepository.LogActivityAsync(createdBy, "experiment_created", $"Created experiment '{title}'")
        Return newId
    End Function

    Public Async Function SetStatusAsync(experimentId As Integer, status As String, actorName As String) As Task
        Await Db.ExecuteAsync(
            "UPDATE experiments SET status = @status WHERE experiment_id = @id",
            New Dictionary(Of String, Object) From {{"@status", status}, {"@id", experimentId}})
        Await UsersRepository.LogActivityAsync(Nothing, "experiment_status_changed", $"{actorName} set experiment #{experimentId} to {status}")
    End Function

    Public Async Function DeleteAsync(experimentId As Integer, actorName As String) As Task
        Await Db.ExecuteAsync(
            "DELETE FROM experiments WHERE experiment_id = @id",
            New Dictionary(Of String, Object) From {{"@id", experimentId}})
        Await UsersRepository.LogActivityAsync(Nothing, "experiment_deleted", $"{actorName} deleted experiment #{experimentId}")
    End Function

    ''' <summary>Which experiments (by id) this student has started/completed, for showing progress on the student screen.</summary>
    Public Async Function GetProgressForUserAsync(userId As Integer) As Task(Of Dictionary(Of Integer, Boolean))
        Const sql As String = "
            SELECT experiment_id, (completed_at IS NOT NULL) AS is_complete
            FROM experiment_completions
            WHERE user_id = @uid"

        Dim rows = Await Db.QueryAsync(Of (Integer, Boolean))(
            sql,
            Function(r) (r.GetInt32("experiment_id"), r.GetBoolean("is_complete")),
            New Dictionary(Of String, Object) From {{"@uid", userId}})

        Dim result As New Dictionary(Of Integer, Boolean)
        For Each row In rows
            result(row.Item1) = row.Item2
        Next
        Return result
    End Function

    Public Async Function MarkStartedAsync(experimentId As Integer, userId As Integer) As Task
        ' INSERT ... ON DUPLICATE KEY UPDATE so clicking "Start" again after
        ' already starting doesn't throw on the unique (experiment_id, user_id) key.
        Await Db.ExecuteAsync(
            "INSERT INTO experiment_completions (experiment_id, user_id, started_at)
             VALUES (@exp, @uid, NOW())
             ON DUPLICATE KEY UPDATE started_at = started_at",
            New Dictionary(Of String, Object) From {{"@exp", experimentId}, {"@uid", userId}})
    End Function

    Public Async Function MarkCompletedAsync(experimentId As Integer, userId As Integer) As Task
        Await Db.ExecuteAsync(
            "INSERT INTO experiment_completions (experiment_id, user_id, started_at, completed_at)
             VALUES (@exp, @uid, NOW(), NOW())
             ON DUPLICATE KEY UPDATE completed_at = NOW()",
            New Dictionary(Of String, Object) From {{"@exp", experimentId}, {"@uid", userId}})
        Await UsersRepository.LogActivityAsync(userId, "experiment_completed", $"Completed experiment #{experimentId}")
    End Function

End Module
