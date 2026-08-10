' ==========================================================================
'  ChemLab Virtual - Teacher Dashboard (VB.NET WinForms replica)
'
'  How to run:
'   1. Create a new "Windows Forms App (.NET Framework)" or ".NET" VB project.
'   2. Delete the default Form1.vb, add this file instead (or paste its
'      contents into a new file called TeacherDashboard.vb).
'   3. Set the Startup Object to "Sub Main" (Project Properties > Application)
'      OR just remove the Module Program block below and instead set the
'      startup form to DashboardForm if you prefer the designer flow.
'   4. Run (F5).
' ==========================================================================

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' --------------------------------------------------------------------------
'  Entry point
' --------------------------------------------------------------------------
Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New DashboardForm())
    End Sub
End Module

' --------------------------------------------------------------------------
'  A Panel that paints itself as a rounded, bordered "card"
' --------------------------------------------------------------------------
Public Class RoundedPanel
    Inherits Panel

    Public Property CornerRadius As Integer = 14
    Public Property CardBackColor As Color = Color.FromArgb(20, 27, 45)
    Public Property BorderColor As Color = Color.FromArgb(38, 47, 68)

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.UserPaint Or
                 ControlStyles.ResizeRedraw Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        BackColor = Color.Transparent
    End Sub

    Private Function RoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d As Integer = radius * 2
        p.AddArc(rect.X, rect.Y, d, d, 180, 90)
        p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Using path As GraphicsPath = RoundedRect(rect, CornerRadius)
            Using bg As New SolidBrush(CardBackColor)
                e.Graphics.FillPath(bg, path)
            End Using
            Using pen As New Pen(BorderColor, 1)
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using
        MyBase.OnPaint(e)
    End Sub
End Class

' --------------------------------------------------------------------------
'  A small rounded "pill" button (used for Grade queue / Assign experiment /
'  the per-row "Grade" links, and the status badges)
' --------------------------------------------------------------------------
Public Class PillLabel
    Inherits Label

    Public Property FillColor As Color = Color.FromArgb(6, 78, 59)
    Public Property TextColorPill As Color = Color.FromArgb(52, 211, 153)

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.UserPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        BackColor = Color.Transparent
        TextAlign = ContentAlignment.MiddleCenter
        Font = New Font("Segoe UI", 8.5F, FontStyle.Regular)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As New GraphicsPath()
            Dim d As Integer = Height
            path.AddArc(rect.X, rect.Y, d, d, 90, 180)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 180)
            path.CloseFigure()
            Using b As New SolidBrush(FillColor)
                e.Graphics.FillPath(b, path)
            End Using
        End Using
        Using sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Using b As New SolidBrush(TextColorPill)
                e.Graphics.DrawString(Text, Font, b, rect, sf)
            End Using
        End Using
    End Sub
End Class

' --------------------------------------------------------------------------
'  Main dashboard form
' --------------------------------------------------------------------------
Public Class DashboardForm
    Inherits Form

    ' ---- palette ----------------------------------------------------
    Private ReadOnly ColBackground As Color = Color.FromArgb(9, 13, 24)
    Private ReadOnly ColCard As Color = Color.FromArgb(18, 24, 41)
    Private ReadOnly ColCardBorder As Color = Color.FromArgb(36, 45, 66)
    Private ReadOnly ColTextPrimary As Color = Color.White
    Private ReadOnly ColTextSecondary As Color = Color.FromArgb(148, 163, 184)
    Private ReadOnly ColAccentTeal As Color = Color.FromArgb(34, 211, 238)
    Private ReadOnly ColAccentPurple As Color = Color.FromArgb(168, 85, 247)
    Private ReadOnly ColGreenBg As Color = Color.FromArgb(6, 78, 59)
    Private ReadOnly ColGreenFg As Color = Color.FromArgb(52, 211, 153)
    Private ReadOnly ColGrayBg As Color = Color.FromArgb(51, 65, 85)
    Private ReadOnly ColGrayFg As Color = Color.FromArgb(203, 213, 225)

    ' data for the "Average by class" chart
    Private ReadOnly ChartLabels As String() = {"11-A", "11-B", "12-C", "12-D"}
    Private ReadOnly ChartValues As Integer() = {75, 80, 85, 65}

    Public Sub New()
        Me.Text = "ChemLab Virtual — Teacher Dashboard"
        Me.Size = New Size(1000, 620)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = ColBackground
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.DoubleBuffered = True

        BuildTitleBar()
        BuildHeader()
        BuildStatCards()
        BuildStudentsTable()
        BuildChartCard()
        BuildGradingQueueCard()
    End Sub

    ' ------------------------------------------------------------------
    Private Sub BuildTitleBar()
        Dim bar As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 30,
            .BackColor = Color.FromArgb(12, 17, 30)
        }
        Dim lbl As New Label With {
            .Text = "ChemLab Virtual — Teacher Dashboard",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 8.5F),
            .AutoSize = True,
            .Location = New Point(12, 7)
        }
        bar.Controls.Add(lbl)
        Me.Controls.Add(bar)
    End Sub

    ' ------------------------------------------------------------------
    Private Sub BuildHeader()
        Dim title As New Label With {
            .Text = "Teacher Dashboard",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(24, 46)
        }
        Dim subtitle As New Label With {
            .Text = "Monitor live lab sessions, assign experiments and review class performance.",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 9.5F),
            .AutoSize = True,
            .Location = New Point(24, 80)
        }
        Me.Controls.Add(title)
        Me.Controls.Add(subtitle)

        ' "Grade queue" outline button
        Dim btnGradeQueue As New Button With {
            .Text = "🗂  Grade queue",
            .Size = New Size(130, 34),
            .Location = New Point(Me.ClientSize.Width - 300, 52),
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = ColTextPrimary,
            .BackColor = Color.FromArgb(24, 31, 50),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnGradeQueue.FlatAppearance.BorderColor = ColCardBorder
        btnGradeQueue.FlatAppearance.BorderSize = 1
        Me.Controls.Add(btnGradeQueue)

        ' "Assign experiment" gradient-look button
        Dim btnAssign As New Button With {
            .Text = "+  Assign experiment",
            .Size = New Size(170, 34),
            .Location = New Point(Me.ClientSize.Width - 160, 52),
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = Color.White,
            .BackColor = ColAccentPurple,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnAssign.FlatAppearance.BorderSize = 0
        Me.Controls.Add(btnAssign)
    End Sub

    ' ------------------------------------------------------------------
    Private Sub BuildStatCards()
        Dim data As (icon As String, title As String, value As String)() = {
            ("👤", "Students enrolled", "128"),
            ("💻", "Live in lab now", "23"),
            ("📋", "Awaiting grading", "11"),
            ("🎓", "Class average", "81%")
        }

        Dim startX As Integer = 24
        Dim y As Integer = 110
        Dim cardW As Integer = 220
        Dim cardH As Integer = 70
        Dim gap As Integer = 12

        For i As Integer = 0 To data.Length - 1
            Dim card As New RoundedPanel With {
                .Location = New Point(startX + i * (cardW + gap), y),
                .Size = New Size(cardW, cardH),
                .CardBackColor = ColCard,
                .BorderColor = ColCardBorder
            }

            Dim iconBadge As New Label With {
                .Text = data(i).icon,
                .Font = New Font("Segoe UI", 12),
                .ForeColor = ColAccentTeal,
                .Size = New Size(34, 34),
                .Location = New Point(14, 18),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            Dim lblTitle As New Label With {
                .Text = data(i).title,
                .ForeColor = ColTextSecondary,
                .Font = New Font("Segoe UI", 8.5F),
                .AutoSize = True,
                .Location = New Point(58, 14)
            }
            Dim lblValue As New Label With {
                .Text = data(i).value,
                .ForeColor = ColTextPrimary,
                .Font = New Font("Segoe UI", 15, FontStyle.Bold),
                .AutoSize = True,
                .Location = New Point(58, 32)
            }

            card.Controls.Add(iconBadge)
            card.Controls.Add(lblTitle)
            card.Controls.Add(lblValue)
            Me.Controls.Add(card)
        Next
    End Sub

    ' ------------------------------------------------------------------
    Private Sub BuildStudentsTable()
        Dim card As New RoundedPanel With {
            .Location = New Point(24, 195),
            .Size = New Size(600, 340),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder
        }

        Dim lblTitle As New Label With {
            .Text = "👥  Students",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(lblTitle)

        Dim grid As New DataGridView With {
            .Location = New Point(14, 48),
            .Size = New Size(572, 275),
            .BackgroundColor = ColCard,
            .BorderStyle = BorderStyle.None,
            .RowHeadersVisible = False,
            .AllowUserToAddRows = False,
            .AllowUserToResizeRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .ColumnHeadersHeight = 28,
            .RowTemplate = New DataGridViewRow With {.Height = 40},
            .EnableHeadersVisualStyles = False,
            .CellBorderStyle = DataGridViewCellBorderStyle.None,
            .GridColor = ColCard,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        }

        grid.ColumnHeadersDefaultCellStyle.BackColor = ColCard
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ColTextSecondary
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8.5F)
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft

        grid.DefaultCellStyle.BackColor = ColCard
        grid.DefaultCellStyle.ForeColor = ColTextPrimary
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 38, 58)
        grid.DefaultCellStyle.SelectionForeColor = ColTextPrimary
        grid.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)

        grid.Columns.Add("Student", "Student")
        grid.Columns.Add("Class", "Class")
        grid.Columns.Add("Completed", "Completed")
        grid.Columns.Add("Average", "Average")
        grid.Columns.Add("Status", "Status")

        grid.Columns("Student").Width = 130
        grid.Columns("Class").Width = 110
        grid.Columns("Completed").Width = 90
        grid.Columns("Average").Width = 80
        grid.Columns("Status").Width = 90

        Dim rows As (name As String, cls As String, completed As String, avg As String, status As String)() = {
            ("Mac Falen", "Grade 11-B", "14 / 20", "86%", "In lab"),
            ("Aisha Bello", "Grade 11-B", "17 / 20", "91%", "In lab"),
            ("Tom Meier", "Grade 11-A", "9 / 20", "68%", "Offline"),
            ("Lina Ortiz", "Grade 11-A", "12 / 20", "79%", "Offline"),
            ("Kwame Adjei", "Grade 12-C", "20 / 20", "94%", "In lab")
        }

        For Each r In rows
            grid.Rows.Add(r.name, r.cls, r.completed, r.avg, r.status)
        Next

        ' custom paint for the Status pill + bold Average column
        AddHandler grid.CellPainting, AddressOf Grid_CellPainting

        card.Controls.Add(grid)
        Me.Controls.Add(card)
    End Sub

    Private Sub Grid_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        Dim grid As DataGridView = DirectCast(sender, DataGridView)
        If e.RowIndex < 0 Then Return

        If grid.Columns(e.ColumnIndex).Name = "Status" AndAlso e.Value IsNot Nothing Then
            e.PaintBackground(e.CellBounds, True)
            Dim status As String = e.Value.ToString()
            Dim isInLab As Boolean = (status = "In lab")
            Dim bg As Color = If(isInLab, ColGreenBg, ColGrayBg)
            Dim fg As Color = If(isInLab, ColGreenFg, ColGrayFg)

            Dim pillW As Integer = 66
            Dim pillH As Integer = 22
            Dim px As Integer = e.CellBounds.X + 8
            Dim py As Integer = e.CellBounds.Y + (e.CellBounds.Height - pillH) \ 2
            Dim rect As New Rectangle(px, py, pillW, pillH)

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using path As New GraphicsPath()
                Dim d As Integer = pillH
                path.AddArc(rect.X, rect.Y, d, d, 90, 180)
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 180)
                path.CloseFigure()
                Using b As New SolidBrush(bg)
                    e.Graphics.FillPath(b, path)
                End Using
            End Using
            Using sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                Using b As New SolidBrush(fg)
                    e.Graphics.DrawString(status, New Font("Segoe UI", 8.5F), b, rect, sf)
                End Using
            End Using
            e.Handled = True
        ElseIf grid.Columns(e.ColumnIndex).Name = "Average" Then
            e.PaintBackground(e.CellBounds, True)
            Using b As New SolidBrush(ColTextPrimary)
                Using sf As New StringFormat With {.Alignment = StringAlignment.Near, .LineAlignment = StringAlignment.Center}
                    e.Graphics.DrawString(If(e.Value?.ToString(), ""), New Font("Segoe UI", 9.5F, FontStyle.Bold), b, e.CellBounds, sf)
                End Using
            End Using
            e.Handled = True
        End If
    End Sub

    ' ------------------------------------------------------------------
    Private Sub BuildChartCard()
        Dim card As New RoundedPanel With {
            .Location = New Point(640, 195),
            .Size = New Size(336, 165),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder
        }
        Dim lblTitle As New Label With {
            .Text = "📊  Average by class",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(lblTitle)

        Dim chartArea As New Panel With {
            .Location = New Point(14, 44),
            .Size = New Size(308, 112),
            .BackColor = Color.Transparent
        }
        AddHandler chartArea.Paint, AddressOf ChartArea_Paint
        card.Controls.Add(chartArea)

        Me.Controls.Add(card)
    End Sub

    Private Sub ChartArea_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim panel As Panel = DirectCast(sender, Panel)
        Dim w As Integer = panel.Width
        Dim h As Integer = panel.Height
        Dim axisH As Integer = 18 ' room for x-axis labels
        Dim maxVal As Integer = 100
        Dim barCount As Integer = ChartValues.Length
        Dim slot As Integer = w \ barCount
        Dim barW As Integer = CInt(slot * 0.42)

        ' gridlines at 0/25/50/75/100
        Using gridPen As New Pen(Color.FromArgb(40, 49, 70))
            For i As Integer = 0 To 4
                Dim gy As Integer = CInt((h - axisH) * i / 4)
                g.DrawLine(gridPen, 0, gy, w, gy)
            Next
        End Using

        Using barBrush As New SolidBrush(ColAccentTeal)
            Using fnt As New Font("Segoe UI", 7.5F)
                Using fg As New SolidBrush(ColTextSecondary)
                    For i As Integer = 0 To barCount - 1
                        Dim val As Integer = ChartValues(i)
                        Dim barH As Integer = CInt((h - axisH) * (val / CDbl(maxVal)))
                        Dim bx As Integer = i * slot + (slot - barW) \ 2
                        Dim by As Integer = (h - axisH) - barH
                        g.FillRectangle(barBrush, bx, by, barW, barH)

                        Dim sf As New StringFormat With {.Alignment = StringAlignment.Center}
                        g.DrawString(ChartLabels(i), fnt, fg, New RectangleF(i * slot, h - axisH + 2, slot, axisH), sf)
                    Next
                End Using
            End Using
        End Using
    End Sub

    ' ------------------------------------------------------------------
    Private Sub BuildGradingQueueCard()
        Dim card As New RoundedPanel With {
            .Location = New Point(640, 372),
            .Size = New Size(336, 163),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder
        }
        Dim lblTitle As New Label With {
            .Text = "🔔  Grading queue",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(lblTitle)

        Dim items As String() = {
            "Titration report — Aisha Bello",
            "Gas Evolution report — Tom Meier",
            "Flame Test quiz — Lina Ortiz"
        }

        Dim y As Integer = 46
        For Each item In items
            Dim row As New Panel With {
                .Location = New Point(14, y),
                .Size = New Size(308, 34),
                .BackColor = Color.FromArgb(24, 31, 50)
            }
            Dim lbl As New Label With {
                .Text = item,
                .ForeColor = ColTextPrimary,
                .Font = New Font("Segoe UI", 8.75F),
                .AutoSize = False,
                .Size = New Size(220, 34),
                .Location = New Point(8, 0),
                .TextAlign = ContentAlignment.MiddleLeft
            }
            Dim btn As New PillLabel With {
                .Text = "⟳ Grade",
                .Size = New Size(66, 22),
                .Location = New Point(234, 6),
                .FillColor = Color.FromArgb(30, 41, 59),
                .TextColorPill = ColAccentTeal,
                .Cursor = Cursors.Hand
            }
            row.Controls.Add(lbl)
            row.Controls.Add(btn)
            card.Controls.Add(row)
            y += 38
        Next

        Me.Controls.Add(card)
    End Sub

End Class