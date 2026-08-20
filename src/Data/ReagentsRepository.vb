Imports System.Drawing

''' <summary>
''' Reads the reagent shelf shown on ChemicalsForm. Returns the same
''' (name, formula, concentration, hazard, color) tuple shape the form
''' already expects, so BuildReagentTable() needs no changes.
''' </summary>
Public Module ReagentsRepository

    Public Async Function GetAllAsync() As Task(Of List(Of (String, String, String, String, Color)))
        Const sql As String = "
            SELECT name, formula, concentration, hazard_class, dot_color_hex
            FROM reagents
            WHERE is_active = 1
            ORDER BY sort_order"

        Return Await Db.QueryAsync(Of (String, String, String, String, Color))(
            sql,
            Function(r) (
                r.GetString("name"),
                r.GetString("formula"),
                r.GetString("concentration"),
                r.GetString("hazard_class"),
                ColorTranslator.FromHtml(r.GetString("dot_color_hex"))
            ))
    End Function

End Module
