Imports System.Threading.Tasks

''' <summary>
''' Backs the admin "System Settings" page. These settings aren't just
''' cosmetic toggles — LoginForm, UsersRepository, and CreateAccountForm all
''' read from here to actually change behavior (see comments on each setting).
''' </summary>
Public Module SettingsRepository

    Public Async Function GetAllAsync() As Task(Of Dictionary(Of String, String))
        Dim rows = Await Db.QueryAsync(Of (String, String))(
            "SELECT setting_key, setting_value FROM system_settings",
            Function(r) (r.GetString("setting_key"), r.GetString("setting_value")))

        Dim result As New Dictionary(Of String, String)
        For Each row In rows
            result(row.Item1) = row.Item2
        Next
        Return result
    End Function

    Public Async Function GetBoolAsync(key As String, defaultValue As Boolean) As Task(Of Boolean)
        Dim rows = Await Db.QueryAsync(Of String)(
            "SELECT setting_value FROM system_settings WHERE setting_key = @key LIMIT 1",
            Function(r) r.GetString("setting_value"),
            New Dictionary(Of String, Object) From {{"@key", key}})
        If rows.Count = 0 Then Return defaultValue
        Return rows(0).Trim().ToLowerInvariant() = "true"
    End Function

    Public Async Function GetStringAsync(key As String, defaultValue As String) As Task(Of String)
        Dim rows = Await Db.QueryAsync(Of String)(
            "SELECT setting_value FROM system_settings WHERE setting_key = @key LIMIT 1",
            Function(r) r.GetString("setting_value"),
            New Dictionary(Of String, Object) From {{"@key", key}})
        If rows.Count = 0 Then Return defaultValue
        Return rows(0)
    End Function

    Public Async Function SetAsync(key As String, value As String, actorName As String) As Task
        Await Db.ExecuteAsync(
            "INSERT INTO system_settings (setting_key, setting_value) VALUES (@key, @val)
             ON DUPLICATE KEY UPDATE setting_value = @val",
            New Dictionary(Of String, Object) From {{"@key", key}, {"@val", value}})
        Await UsersRepository.LogActivityAsync(Nothing, "setting_changed", $"{actorName} set '{key}' to '{value}'")
    End Function

End Module
