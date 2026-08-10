' ==========================================================================
'  ChemLab Virtual - Lab Notebook (VB.NET WinForms replica)
' ==========================================================================

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New NotebookForm())
    End Sub
End Module

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
        Font = New Font("Segoe UI", 8F, FontStyle.Regular)
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

Public Class ContentBox
    Inherits RoundedPanel

    Private lblText As New Label()

    Public Sub New()
        CornerRadius = 10
        CardBackColor = Color.FromArgb(15, 21, 36)
        BorderColor = Color.FromArgb(36, 45, 66)

        lblText.AutoSize = False
        lblText.Dock = DockStyle.Fill
        lblText.Padding = New Padding(14, 10, 14, 10)
        lblText.ForeColor = Color.FromArgb(226, 232, 240)
        lblText.Font = New Font("Segoe UI", 9.25F)
        lblText.TextAlign = ContentAlignment.TopLeft
        Controls.Add(lblText)
    End Sub

    Public Property ContentText As String
        Get
            Return lblText.Text
        End Get
        Set(value As String)
            lblText.Text = value
        End Set
    End Property
End Class

Public Class NotebookForm
    Inherits Form

    Private ReadOnly ColBackground As Color = Color.FromArgb(9, 13, 24)
    Private ReadOnly ColCard As Color = Color.FromArgb(18, 24, 41)
    Private ReadOnly ColCardBorder As Color = Color.FromArgb(36, 45, 66)
    Private ReadOnly ColTextPrimary As Color = Color.White
    Private ReadOnly ColTextSecondary As Color = Color.FromArgb(148, 163, 184)
    Private ReadOnly ColAccentTeal As Color = Color.FromArgb(34, 211, 238)
    Private ReadOnly ColAccentPurple As Color = Color.FromArgb(168, 85, 247)
    Private ReadOnly ColGreenBg As Color = Color.FromArgb(6, 78, 59)
    Private ReadOnly ColGreenFg As Color = Color.FromArgb(52, 211, 153)
    Private ReadOnly ColOrangeBg As Color = Color.FromArgb(66, 46, 15)
    Private ReadOnly ColOrangeFg As Color = Color.FromArgb(251, 191, 36)

    Private ReadOnly Entries As (title As String, date As String, status As String)() = {
        ("Acid & Base Reaction", "12 Mar", "Submitted"),
        ("Gas Evolution", "09 Mar", "Draft"),
        ("Flame Test", "04 Mar", "Submitted")
    }
    Private selectedIndex As Integer = 0

    Private entryListPanel As Panel
    Private detailCard As RoundedPanel
    Private detailTitleLabel As Label
    Private detailScroll As Panel

    Public Sub New()
        Me.Text = "ChemLab Virtual — Lab Notebook"
        Me.Size = New Size(1000, 620)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = ColBackground
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.DoubleBuffered = True

        BuildTitleBar()
        BuildHeader()
        BuildEntriesCard()
        BuildDetailCard()
        RenderDetail(selectedIndex)
    End Sub

    Private Sub BuildTitleBar()
        Dim bar As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 30,
            .BackColor = Color.FromArgb(12, 17, 30)
        }
        Dim lbl As New Label With {
            .Text = "ChemLab Virtual — Lab Notebook",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 8.5F),
            .AutoSize = True,
            .Location = New Point(12, 7)
        }
        bar.Controls.Add(lbl)
        Me.Controls.Add(bar)
    End Sub

    Private Sub BuildHeader()
        Dim title As New Label With {
            .Text = "Lab Notebook",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(24, 46)
        }
        Dim subtitle As New Label With {
            .Text = "Record aim, method, observations and conclusions for every experiment.",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 9.5F),
            .AutoSize = True,
            .Location = New Point(24, 80)
        }
        Me.Controls.Add(title)
        Me.Controls.Add(subtitle)

        Dim btnExport As New Button With {
            .Text = "⬇  Export PDF",
            .Size = New Size(120, 34),
            .Location = New Point(Me.ClientSize.Width - 270, 52),
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = ColTextPrimary,
            .BackColor = Color.FromArgb(24, 31, 50),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnExport.FlatAppearance.BorderColor = ColCardBorder
        btnExport.FlatAppearance.BorderSize = 1
        Me.Controls.Add(btnExport)

        Dim btnSubmit As New Button With {
            .Text = "Submit Report",
            .Size = New Size(140, 34),
            .Location = New Point(Me.ClientSize.Width - 140, 52),
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = Color.White,
            .BackColor = ColAccentPurple,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnSubmit.FlatAppearance.BorderSize = 0
        Me.Controls.Add(btnSubmit)
    End Sub

    Private Sub BuildEntriesCard()
        Dim card As New RoundedPanel With {
            .Location = New Point(24, 110),
            .Size = New Size(152, 410),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder
        }
        Dim lblTitle As New Label With {
            .Text = "📖  Entries",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        card.Controls.Add(lblTitle)

        entryListPanel = New Panel With {
            .Location = New Point(8, 46),
            .Size = New Size(136, 356),
            .BackColor = Color.Transparent
        }
        card.Controls.Add(entryListPanel)
        Me.Controls.Add(card)

        RenderEntryList()
    End Sub

    Private Sub RenderEntryList()
        entryListPanel.Controls.Clear()
        Dim y As Integer = 0
        For i As Integer = 0 To Entries.Length - 1
            Dim e = Entries(i)
            Dim isSelected As Boolean = (i = selectedIndex)

            Dim row As New Panel With {
                .Location = New Point(0, y),
                .Size = New Size(136, 60),
                .BackColor = If(isSelected, Color.FromArgb(41, 30, 74), Color.Transparent),
                .Tag = i,
                .Cursor = Cursors.Hand
            }

            Dim lblName As New Label With {
                .Text = e.title,
                .ForeColor = ColTextPrimary,
                .Font = New Font("Segoe UI", 9F, FontStyle.Bold),
                .AutoSize = False,
                .Size = New Size(126, 18),
                .Location = New Point(6, 6)
            }
            Dim lblDate As New Label With {
                .Text = e.date,
                .ForeColor = ColTextSecondary,
                .Font = New Font("Segoe UI", 8F),
                .AutoSize = True,
                .Location = New Point(6, 26)
            }
            Dim isSubmitted As Boolean = (e.status = "Submitted")
            Dim pill As New PillLabel With {
                .Text = e.status,
                .Size = New Size(If(isSubmitted, 68, 46), 20),
                .Location = New Point(6, 34),
                .FillColor = If(isSubmitted, ColGreenBg, ColOrangeBg),
                .TextColorPill = If(isSubmitted, ColGreenFg, ColOrangeFg)
            }

            row.Controls.Add(lblName)
            row.Controls.Add(lblDate)
            row.Controls.Add(pill)

            AddHandler row.Click, AddressOf EntryRow_Click
            AddHandler lblName.Click, AddressOf EntryRow_Click
            AddHandler lblDate.Click, AddressOf EntryRow_Click
            row.Tag = i
            lblName.Tag = i
            lblDate.Tag = i

            entryListPanel.Controls.Add(row)
            y += 64
        Next
    End Sub

    Private Sub EntryRow_Click(sender As Object, e As EventArgs)
        Dim ctrl As Control = DirectCast(sender, Control)
        Dim idx As Integer = CInt(ctrl.Tag)
        selectedIndex = idx
        RenderEntryList()
        RenderDetail(idx)
    End Sub

    Private Sub BuildDetailCard()
        detailCard = New RoundedPanel With {
            .Location = New Point(192, 110),
            .Size = New Size(784, 410),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        }

        detailTitleLabel = New Label With {
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(16, 14)
        }
        detailCard.Controls.Add(detailTitleLabel)

        Dim savedPill As New PillLabel With {
            .Text = "Auto-saved",
            .Size = New Size(84, 22),
            .Location = New Point(detailCard.Width - 100, 14),
            .FillColor = ColGreenBg,
            .TextColorPill = ColGreenFg,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        detailCard.Controls.Add(savedPill)

        detailScroll = New Panel With {
            .Location = New Point(14, 48),
            .Size = New Size(detailCard.Width - 28, detailCard.Height - 62),
            .AutoScroll = True,
            .BackColor = Color.Transparent,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        }
        detailCard.Controls.Add(detailScroll)

        Me.Controls.Add(detailCard)
    End Sub

    Private Sub RenderDetail(idx As Integer)
        Dim e = Entries(idx)
        detailTitleLabel.Text = "🧪  " & e.title & " — " & FullDate(e.date)

        detailScroll.Controls.Clear()

        Dim sections As (heading As String, body As String, lines As Integer)() = {
            ("AIM", "To investigate the neutralization of hydrochloric acid by sodium hydroxide.", 1),
            ("APPARATUS & CHEMICALS", "Conical flask (250 ml), beaker (500 ml), clamp stand, thermometer, 1.0 M HCl, 1.0 M NaOH, phenolphthalein.", 1),
            ("METHOD", "50 ml of HCl was measured into the conical flask. 50 ml of NaOH was added slowly while stirring. Temperature was recorded every 15 s.", 2),
            ("OBSERVATIONS", "Solution turned from blue to purple. Temperature rose from 21.0 °C to 28.5 °C over 120 s.", 2),
            ("CONCLUSION", "The reaction is exothermic and produces sodium chloride and water.", 1)
        }

        Dim y As Integer = 0
        Dim width As Integer = detailScroll.Width - 20

        For Each sec In sections
            Dim lblHeading As New Label With {
                .Text = sec.heading,
                .ForeColor = ColTextSecondary,
                .Font = New Font("Segoe UI", 8.25F, FontStyle.Bold),
                .AutoSize = True,
                .Location = New Point(2, y)
            }
            detailScroll.Controls.Add(lblHeading)
            y += 22

            Dim boxHeight As Integer = If(sec.lines = 1, 44, 62)
            Dim box As New ContentBox With {
                .Location = New Point(2, y),
                .Size = New Size(width, boxHeight),
                .ContentText = sec.body,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            }
            detailScroll.Controls.Add(box)
            y += boxHeight + 20
        Next
    End Sub

    Private Function FullDate(shortDate As String) As String
        Return shortDate & " 2026"
    End Function

End Class