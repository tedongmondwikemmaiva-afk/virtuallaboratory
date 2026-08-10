' ==========================================================================
'  ChemLab Virtual - Sign Out Confirmation Dialog (VB.NET WinForms replica)
' ==========================================================================

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New SignOutForm())
    End Sub
End Module

' --------------------------------------------------------------------------
'  A Panel that paints itself as a rounded, bordered "card"
' --------------------------------------------------------------------------
Public Class RoundedPanel
    Inherits Panel

    Public Property CornerRadius As Integer = 20
    Public Property CardBackColor As Color = Color.FromArgb(24, 30, 48)
    Public Property BorderColor As Color = Color.FromArgb(50, 58, 82)

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
'  A rounded button that supports a flat solid fill OR a left-to-right
'  gradient fill (used for the purple/blue "Sign out" button)
' --------------------------------------------------------------------------
Public Class RoundedButton
    Inherits Button

    Public Property CornerRadius As Integer = 22
    Public Property UseGradient As Boolean = False
    Public Property GradientStart As Color = Color.FromArgb(99, 102, 241)
    Public Property GradientEnd As Color = Color.FromArgb(168, 85, 247)
    Public Property FillColor As Color = Color.FromArgb(30, 37, 58)
    Public Property BorderColorNormal As Color = Color.FromArgb(55, 63, 88)

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.UserPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
        BackColor = Color.Transparent
        ForeColor = Color.White
        Cursor = Cursors.Hand
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
        Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = RoundedRect(rect, CornerRadius)
            If UseGradient Then
                Using brush As New LinearGradientBrush(rect, GradientStart, GradientEnd, LinearGradientMode.Horizontal)
                    e.Graphics.FillPath(brush, path)
                End Using
            Else
                Using brush As New SolidBrush(FillColor)
                    e.Graphics.FillPath(brush, path)
                End Using
                Using pen As New Pen(BorderColorNormal, 1)
                    e.Graphics.DrawPath(pen, path)
                End Using
            End If
        End Using
        Using sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Using b As New SolidBrush(ForeColor)
                e.Graphics.DrawString(Text, Font, b, rect, sf)
            End Using
        End Using
    End Sub
End Class

' --------------------------------------------------------------------------
'  Main form — full-screen dark backdrop with faint blurred bar shapes,
'  and a centered rounded confirmation card
' --------------------------------------------------------------------------
Public Class SignOutForm
    Inherits Form

    Private ReadOnly ColBackground As Color = Color.FromArgb(8, 11, 22)
    Private ReadOnly ColCard As Color = Color.FromArgb(22, 28, 46)
    Private ReadOnly ColCardBorder As Color = Color.FromArgb(48, 56, 80)
    Private ReadOnly ColTextPrimary As Color = Color.White
    Private ReadOnly ColTextSecondary As Color = Color.FromArgb(148, 163, 184)
    Private ReadOnly ColIconCircle As Color = Color.FromArgb(60, 24, 30)
    Private ReadOnly ColIconGlyph As Color = Color.FromArgb(248, 113, 113)

    Public Sub New()
        Me.Text = "ChemLab Virtual — Sign out"
        Me.Size = New Size(760, 400)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = ColBackground
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = FormBorderStyle.None

        AddHandler Me.Paint, AddressOf Form_Paint
        BuildCard()

        ' allow dragging the borderless window and Esc to close
        AddHandler Me.KeyDown, Sub(s, e2) If e2.KeyCode = Keys.Escape Then Me.Close()
        Me.KeyPreview = True
    End Sub

    ' faint blurred rectangles behind the modal, mimicking the blurred
    ' dashboard bar chart visible in the screenshot
    Private Sub Form_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim bars As (x As Integer, top As Integer, w As Integer, colorAlpha As Integer)() = {
            (40, 60, 60, 40),
            (130, 20, 70, 55),
            (230, 90, 50, 35),
            (320, 40, 65, 60),
            (420, 70, 55, 30),
            (520, 10, 75, 50),
            (630, 55, 60, 40)
        }
        For Each bar In bars
            Using b As New SolidBrush(Color.FromArgb(bar.colorAlpha, 76, 90, 170))
                g.FillRectangle(b, bar.x, bar.top, bar.w, Me.Height - bar.top - 40)
            End Using
        Next
    End Sub

    Private Sub BuildCard()
        Dim card As New RoundedPanel With {
            .Size = New Size(370, 260),
            .CardBackColor = ColCard,
            .BorderColor = ColCardBorder,
            .CornerRadius = 22,
            .Anchor = AnchorStyles.None
        }
        card.Location = New Point((Me.ClientSize.Width - card.Width) \ 2, (Me.ClientSize.Height - card.Height) \ 2)

        ' icon circle
        Dim iconCircle As New Panel With {
            .Size = New Size(56, 56),
            .Location = New Point((card.Width - 56) \ 2, 26),
            .BackColor = Color.Transparent
        }
        AddHandler iconCircle.Paint, AddressOf IconCircle_Paint
        card.Controls.Add(iconCircle)

        ' title
        Dim lblTitle As New Label With {
            .Text = "Sign out of ChemLab?",
            .ForeColor = ColTextPrimary,
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .AutoSize = False,
            .Size = New Size(card.Width - 20, 26),
            .Location = New Point(10, 96),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        card.Controls.Add(lblTitle)

        ' subtitle (two lines, centered)
        Dim lblSubtitle As New Label With {
            .Text = "Unsaved notebook entries and bench state will" & vbCrLf & "be stored locally.",
            .ForeColor = ColTextSecondary,
            .Font = New Font("Segoe UI", 9F),
            .AutoSize = False,
            .Size = New Size(card.Width - 20, 40),
            .Location = New Point(10, 128),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        card.Controls.Add(lblSubtitle)

        ' buttons
        Dim btnCancel As New RoundedButton With {
            .Text = "Cancel",
            .Size = New Size(150, 42),
            .Location = New Point(28, 192),
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .UseGradient = False,
            .FillColor = Color.FromArgb(30, 37, 58),
            .BorderColorNormal = Color.FromArgb(55, 63, 88),
            .CornerRadius = 21
        }
        AddHandler btnCancel.Click, Sub() Me.Close()

        Dim btnSignOut As New RoundedButton With {
            .Text = "Sign out",
            .Size = New Size(150, 42),
            .Location = New Point(192, 192),
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .UseGradient = True,
            .GradientStart = Color.FromArgb(99, 102, 241),
            .GradientEnd = Color.FromArgb(168, 85, 247),
            .CornerRadius = 21
        }
        AddHandler btnSignOut.Click, Sub()
                                          MessageBox.Show("Signed out.", "ChemLab", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                          Me.Close()
                                      End Sub

        card.Controls.Add(btnCancel)
        card.Controls.Add(btnSignOut)

        Me.Controls.Add(card)
    End Sub

    Private Sub IconCircle_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim panel As Panel = DirectCast(sender, Panel)
        Dim rect As New Rectangle(0, 0, panel.Width - 1, panel.Height - 1)

        Using bg As New SolidBrush(ColIconCircle)
            g.FillEllipse(bg, rect)
        End Using

        ' simple door/exit-arrow glyph drawn with lines
        Using pen As New Pen(ColIconGlyph, 2)
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            ' door bracket "[" shape
            g.DrawLine(pen, 22, 16, 16, 16)
            g.DrawLine(pen, 16, 16, 16, 40)
            g.DrawLine(pen, 16, 40, 22, 40)
            ' arrow shaft
            g.DrawLine(pen, 20, 28, 40, 28)
            ' arrow head
            g.DrawLine(pen, 34, 22, 40, 28)
            g.DrawLine(pen, 34, 34, 40, 28)
        End Using
    End Sub

End Class