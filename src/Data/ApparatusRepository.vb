''' <summary>
''' Reads the equipment shelf shown on ApparatusForm. Returns the same
''' (name, capacity, status) tuple shape the form already expects, so
''' BuildContent()/BuildCardsGrid() need no changes.
''' </summary>
Public Module ApparatusRepository

    Public Async Function GetAllAsync() As Task(Of List(Of (String, String, String)))
        Const sql As String = "
            SELECT name, capacity, status
            FROM apparatus
            WHERE is_active = 1
            ORDER BY sort_order"

        Return Await Db.QueryAsync(Of (String, String, String))(
            sql,
            Function(r) (
                r.GetString("name"),
                If(r.IsDBNull(r.GetOrdinal("capacity")), "—", r.GetString("capacity")),
                r.GetString("status")
            ))
    End Function

End Module
