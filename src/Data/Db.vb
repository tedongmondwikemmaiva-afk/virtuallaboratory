Imports System.Collections.Generic
Imports System.Configuration
Imports System.Threading.Tasks
Imports MySqlConnector

''' <summary>
''' Thin Data Access Layer around MySqlConnector. Every method opens its own
''' short-lived connection (connection pooling handles reuse under the hood,
''' so this is fine — don't try to share one long-lived connection across
''' the app). All queries are parameterized; never string-concatenate user
''' input into SQL.
''' </summary>
Public Module Db

    Private ReadOnly ConnString As String =
        ConfigurationManager.ConnectionStrings("ChemLabDb").ConnectionString

    Private Function OpenConnection() As MySqlConnection
        Dim conn As New MySqlConnection(ConnString)
        conn.Open()
        Return conn
    End Function

    ''' <summary>Runs a SELECT and maps each row via <paramref name="map"/>.</summary>
    Public Async Function QueryAsync(Of T)(sql As String, map As Func(Of MySqlDataReader, T),
                                            Optional params As Dictionary(Of String, Object) = Nothing) As Task(Of List(Of T))
        Dim results As New List(Of T)
        Using conn = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                AddParams(cmd, params)
                Using reader = Await cmd.ExecuteReaderAsync()
                    While Await reader.ReadAsync()
                        results.Add(map(reader))
                    End While
                End Using
            End Using
        End Using
        Return results
    End Function

    ''' <summary>Runs an INSERT/UPDATE/DELETE and returns the number of affected rows.</summary>
    Public Async Function ExecuteAsync(sql As String,
                                        Optional params As Dictionary(Of String, Object) = Nothing) As Task(Of Integer)
        Using conn = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                AddParams(cmd, params)
                Return Await cmd.ExecuteNonQueryAsync()
            End Using
        End Using
    End Function

    ''' <summary>Runs an INSERT and returns the new row's auto-increment id.</summary>
    Public Async Function ExecuteInsertAsync(sql As String,
                                              Optional params As Dictionary(Of String, Object) = Nothing) As Task(Of Long)
        Using conn = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                AddParams(cmd, params)
                Await cmd.ExecuteNonQueryAsync()
                Return cmd.LastInsertedId
            End Using
        End Using
    End Function

    ''' <summary>Runs a query that returns a single scalar value (COUNT, single column, etc.).</summary>
    Public Async Function ScalarAsync(Of T)(sql As String,
                                             Optional params As Dictionary(Of String, Object) = Nothing) As Task(Of T)
        Using conn = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                AddParams(cmd, params)
                Dim result = Await cmd.ExecuteScalarAsync()
                If result Is Nothing OrElse result Is DBNull.Value Then Return Nothing
                Return DirectCast(Convert.ChangeType(result, GetType(T)), T)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Runs several statements as one transaction. Use this whenever more than
    ''' one write must succeed or fail together (e.g. saving a quiz attempt
    ''' header plus its per-question answers).
    ''' </summary>
    Public Async Function RunInTransactionAsync(work As Func(Of MySqlConnection, MySqlTransaction, Task)) As Task
        Using conn = OpenConnection()
            Using tx = Await conn.BeginTransactionAsync()
                ' VB doesn't allow Await inside a Catch block, so capture the
                ' exception here and do the (async) rollback/rethrow outside it.
                Dim caught As Exception = Nothing
                Try
                    Await work(conn, tx)
                Catch ex As Exception
                    caught = ex
                End Try

                If caught Is Nothing Then
                    Await tx.CommitAsync()
                Else
                    Await tx.RollbackAsync()
                    Throw caught
                End If
            End Using
        End Using
    End Function

    Private Sub AddParams(cmd As MySqlCommand, params As Dictionary(Of String, Object))
        If params Is Nothing Then Return
        For Each kv In params
            cmd.Parameters.AddWithValue(kv.Key, If(kv.Value, DBNull.Value))
        Next
    End Sub

End Module
