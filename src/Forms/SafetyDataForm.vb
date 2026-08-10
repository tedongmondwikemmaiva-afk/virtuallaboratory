' ==========================================================================
'  ChemLab Virtual - Safety Data Sheets (VB.NET WinForms replica)
' ==========================================================================

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New SafetyDataForm())
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

Public Class SafetyDataForm
    Inherits Form

    Private ReadOnly ColBackground As Color = Color.FromArgb(9, 13, 24)
    Private ReadOnly ColCard As Color = Color.FromArgb(18, 24, 41)
    Private ReadOnly ColCardBorder As Color = Color.FromArgb(36, 45, 66)
    Private ReadOnly ColInnerCard As Color = Color.FromArgb(15, 21, 36)
    Private ReadOnly ColTextPrimary As Color = Color.White
    Private ReadOnly ColTextSecondary As Color = Color.FromArgb(148, 163, 184)
    Private ReadOnly ColAccentTeal As Color = Color.FromArgb(34, 211, 238)
    Private ReadOnly ColAccentPurple As Color = Color.FromArgb(168, 85, 247)
    Private ReadOnly ColSelected As Color = Color.FromArgb(147, 51, 234)
    Private ReadOnly ColOrangeBg As Color = Color.FromArgb(66, 46, 15)
    Private ReadOnly ColOrangeFg As Color = Color.FromArgb(251, 191, 36)
    Private ReadOnly ColTealBg As Color = Color.FromArgb(12, 60, 68)
    Private ReadOnly ColTealFg As Color = Color.FromArgb(45, 212, 191)
    Private ReadOnly ColGrayBg As Color = Color.FromArgb(51, 65, 85)
    Private ReadOnly ColGrayFg As Color = Color.FromArgb(203, 213, 225)

    Private ReadOnly Reagents As (name As String, formula As String)() = {
        ("Hydrochloric Acid", "HCl"),
        ("Sodium Hydroxide", "NaOH"),
        ("Copper(II) Sulfate", "CuSO₄"),
        ("Ethanol", "C₂H₅OH"),
        ("Silver Nitrate", "AgNO₃"),
        ("Distilled Water", "H₂O")
    }
    Private selectedIndex As Integer = 0

    Private reagentListPanel As Panel
    Private detailContainer As Panel

    Public Sub New()
        Me.Text = "ChemLab Virtual — Safety Data"
        Me.Size = New Size(1000, 620)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = ColBackground
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.DoubleBuffered = True

        BuildTitleBar()
        BuildHeader()
        BuildReagentListCard()
        BuildDetailArea()
        RenderDetail(selectedIndex)
    End Sub

    Private Sub BuildTitleBar()
        Dim bar As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 30,
            .BackColor = Color.FromArgb(12, 17, 30)
        }
        Dim lbl As New Label With {
            .Text = "ChemLab Virtual — Safety Data",
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
            .Text = "Safety Data Sheets",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(24, 46)
        }
        Dim subtitle As New Label With {
            .Text = "Hazard information, handling rules and first-aid for every reagent on the shelf.",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 9.5F),
            .AutoSize = True,
            .Location = New Point(24, 80)
        }
        Me.Controls.Add(title)
        Me.Controls.Add(subtitle)

        Dim btnPrint As New Button With {
            .Text = "⎙  Print SDS",
            .Size = New Size(120, 34),
            .Location = New Point(Me.ClientSize.Width - 140, 52),
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = Color.White,
            .BackColor = ColAccentPurple,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnPrint.FlatAppearance.BorderSize = 0
        Me.Controls.Add(btnPrint)
    End Sub

    Private Sub BuildReagentListCard()
        Dim card As New RoundedPanel With {
            .Location = New Point(24, 110),
            .Size = New Size(180, 410),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder
        }

        Dim searchBox As New TextBox With {
            .Location = New Point(12, 12),
            .Size = New Size(156, 24),
            .BackColor = Color.FromArgb(15, 21, 36),
            .ForeColor = ColTextPrimary,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Dim placeholder As New Label With {
            .Text = "🔍 Search reagent...",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 8F),
            .AutoSize = True,
            .Location = New Point(16, 16),
            .BackColor = Color.Transparent,
            .Enabled = False
        }
        card.Controls.Add(searchBox)
        card.Controls.Add(placeholder)

        reagentListPanel = New Panel With {
            .Location = New Point(6, 46),
            .Size = New Size(168, 356),
            .BackColor = Color.Transparent
        }
        card.Controls.Add(reagentListPanel)
        Me.Controls.Add(card)

        RenderReagentList()
    End Sub

    Private Sub RenderReagentList()
        reagentListPanel.Controls.Clear()
        Dim y As Integer = 0
        For i As Integer = 0 To Reagents.Length - 1
            Dim r = Reagents(i)
            Dim isSelected As Boolean = (i = selectedIndex)

            Dim row As New RoundedPanel With {
                .Location = New Point(0, y),
                .Size = New Size(168, 52),
                .CornerRadius = 8,
                .CardBackColor = If(isSelected, ColSelected, Color.Transparent),
                .BorderColor = If(isSelected, ColSelected, Color.Transparent),
                .Tag = i,
                .Cursor = Cursors.Hand
            }

            Dim lblName As New Label With {
                .Text = r.name,
                .ForeColor = ColTextPrimary,
                .Font = New Font("Segoe UI", 9F, FontStyle.Bold),
                .AutoSize = False,
                .Size = New Size(130, 18),
                .Location = New Point(10, 8),
                .BackColor = Color.Transparent
            }
            Dim lblFormula As New Label With {
                .Text = r.formula,
                .ForeColor = If(isSelected, Color.FromArgb(233, 213, 255), ColTextSecondary),
                .Font = New Font("Segoe UI", 8F),
                .AutoSize = True,
                .Location = New Point(10, 28),
                .BackColor = Color.Transparent
            }
            Dim lblIcon As New Label With {
                .Text = "⚠",
                .ForeColor = If(isSelected, Color.White, Color.FromArgb(234, 179, 8)),
                .Font = New Font("Segoe UI", 10F),
                .AutoSize = True,
                .Location = New Point(146, 16),
                .BackColor = Color.Transparent
            }

            row.Controls.Add(lblName)
            row.Controls.Add(lblFormula)
            row.Controls.Add(lblIcon)

            AddHandler row.Click, AddressOf ReagentRow_Click
            AddHandler lblName.Click, AddressOf ReagentRow_Click
            AddHandler lblFormula.Click, AddressOf ReagentRow_Click
            row.Tag = i
            lblName.Tag = i
            lblFormula.Tag = i

            reagentListPanel.Controls.Add(row)
            y += 56
        Next
    End Sub

    Private Sub ReagentRow_Click(sender As Object, e As EventArgs)
        Dim ctrl As Control = DirectCast(sender, Control)
        Dim idx As Integer = CInt(ctrl.Tag)
        selectedIndex = idx
        RenderReagentList()
        RenderDetail(idx)
    End Sub

    Private Sub BuildDetailArea()
        detailContainer = New Panel With {
            .Location = New Point(220, 110),
            .Size = New Size(756, 410),
            .BackColor = Color.Transparent,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        }
        Me.Controls.Add(detailContainer)
    End Sub

    Private Sub RenderDetail(idx As Integer)
        detailContainer.Controls.Clear()
        Dim r = Reagents(idx)

        ' ---- top info card ----
        Dim topCard As New RoundedPanel With {
            .Location = New Point(0, 0),
            .Size = New Size(756, 130),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        }

        Dim lblTitle As New Label With {
            .Text = r.name & " — " & r.formula,
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(16, 12),
            .BackColor = Color.Transparent
        }
        Dim lblSub As New Label With {
            .Text = "1.0 M aqueous solution · CAS 7647-01-0 · Revision 4 · 02 Feb 2026",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 8.5F),
            .AutoSize = True,
            .Location = New Point(16, 40),
            .BackColor = Color.Transparent
        }
        topCard.Controls.Add(lblTitle)
        topCard.Controls.Add(lblSub)

        Dim hazardPill1 As New PillLabel With {
            .Text = "⚠ Corrosive",
            .Size = New Size(80, 22),
            .Location = New Point(topCard.Width - 300, 14),
            .FillColor = ColOrangeBg,
            .TextColorPill = ColOrangeFg,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        Dim hazardPill2 As New PillLabel With {
            .Text = "◉ Eye damage",
            .Size = New Size(96, 22),
            .Location = New Point(topCard.Width - 214, 14),
            .FillColor = ColTealBg,
            .TextColorPill = ColTealFg,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        Dim hazardPill3 As New PillLabel With {
            .Text = "GHS05",
            .Size = New Size(62, 22),
            .Location = New Point(topCard.Width - 112, 14),
            .FillColor = ColGrayBg,
            .TextColorPill = ColGrayFg,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        topCard.Controls.Add(hazardPill1)
        topCard.Controls.Add(hazardPill2)
        topCard.Controls.Add(hazardPill3)

        ' three inner info boxes: Storage / Concentration / Flammability
        Dim infoData As (icon As String, label As String, value As String)() = {
            ("🌡", "Storage", "15–25 °C, ventilated acid cabinet"),
            ("⚗", "Concentration", "1.0 mol/dm³ (≈3.6% w/w)"),
            ("🔥", "Flammability", "Non-flammable, reacts with metals")
        }
        Dim boxW As Integer = 240
        For i As Integer = 0 To infoData.Length - 1
            Dim infoBox As New RoundedPanel With {
                .Location = New Point(16 + i * (boxW + 8), 74),
                .Size = New Size(boxW, 46),
                .CornerRadius = 8,
                .CardBackColor = ColInnerCard,
                .BorderColor = ColCardBorder
            }
            Dim lblIcon As New Label With {
                .Text = infoData(i).icon,
                .ForeColor = ColAccentTeal,
                .Font = New Font("Segoe UI", 9F),
                .AutoSize = True,
                .Location = New Point(10, 6),
                .BackColor = Color.Transparent
            }
            Dim lblLabel As New Label With {
                .Text = infoData(i).label,
                .ForeColor = ColTextSecondary,
                .Font = New Font("Segoe UI", 7.5F),
                .AutoSize = True,
                .Location = New Point(28, 6),
                .BackColor = Color.Transparent
            }
            Dim lblValue As New Label With {
                .Text = infoData(i).value,
                .ForeColor = ColTextPrimary,
                .Font = New Font("Segoe UI", 8.25F, FontStyle.Bold),
                .AutoSize = False,
                .Size = New Size(boxW - 20, 26),
                .Location = New Point(10, 20),
                .BackColor = Color.Transparent
            }
            infoBox.Controls.Add(lblIcon)
            infoBox.Controls.Add(lblLabel)
            infoBox.Controls.Add(lblValue)
            topCard.Controls.Add(infoBox)
        Next

        detailContainer.Controls.Add(topCard)

        ' ---- Handling & PPE card ----
        Dim handlingCard As New RoundedPanel With {
            .Location = New Point(0, 140),
            .Size = New Size(374, 270),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or