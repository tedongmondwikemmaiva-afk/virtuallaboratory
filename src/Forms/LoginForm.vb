Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' Shared helper for rounded-rectangle paths, used by all custom-painted controls below.
Module UIHelpers
    Public Function RoundedRectPath(bounds As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()

        ' Guard: if bounds are empty or radius is non-positive, avoid AddArc calls
        If bounds.Width <= 0 OrElse bounds.Height <= 0 Then
            Return path
        End If

        If radius <= 0 Then
            path.AddRectangle(bounds)
            Return path
        End If

        Dim d As Integer = radius * 2
        If d > bounds.Width Then d = bounds.Width
        If d > bounds.Height Then d = bounds.Height

        ' If the calculated diameter is too small for arcs, fall back to rectangle
        If d <= 1 Then
            path.AddRectangle(bounds)
            Return path
        End If

        path.AddArc(New Rectangle(bounds.X, bounds.Y, d, d), 180.0F, 90.0F)
        path.AddArc(New Rectangle(bounds.Right - d, bounds.Y, d, d), 270.0F, 90.0F)
        path.AddArc(New Rectangle(bounds.Right - d, bounds.Bottom - d, d, d), 0.0F, 90.0F)
        path.AddArc(New Rectangle(bounds.X, bounds.Bottom - d, d, d), 90.0F, 90.0F)
        path.CloseFigure()
        Return path
    End Function
End Module

' Rounded card / input container. Set BackColor to match the parent so the
' corners outside the rounded path blend in seamlessly (no true transparency needed).
Public Class RoundedPanel
    Inherits Panel

    Public Property CornerRadius As Integer = 14
    Public Property FillColor As Color = Color.FromArgb(20, 24, 42)
    Public Property BorderColor As Color = Color.FromArgb(45, 50, 74)
    Public Property DrawBorder As Boolean = True

    Public Sub New()
        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Using path As GraphicsPath = RoundedRectPath(rect, CornerRadius)
            Using fillBrush As New SolidBrush(FillColor)
                g.FillPath(fillBrush, path)
            End Using
            If DrawBorder Then
                Using pen As New Pen(BorderColor, 1)
                    g.DrawPath(pen, path)
                End Using
            End If
        End Using
        ' Ensure any external Paint event handlers attached to this control are invoked
        ' (overriding OnPaint prevents the base implementation from raising the Paint event).
        MyBase.OnPaint(e)
    End Sub
End Class

' Primary gradient action button ("Enter the Lab").
Public Class GradientButton
    Inherits Button

    Public Property ColorStart As Color = Color.FromArgb(108, 92, 231)
    Public Property ColorEnd As Color = Color.FromArgb(214, 82, 205)

    Public Sub New()
        Me.FlatStyle = FlatStyle.Flat
        Me.FlatAppearance.BorderSize = 0
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        Me.Cursor = Cursors.Hand
        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Using path As GraphicsPath = RoundedRectPath(rect, 10)
            Using br As New LinearGradientBrush(rect, ColorStart, ColorEnd, 0.0F)
                g.FillPath(br, path)
            End Using
        End Using
        Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
        g.DrawString(Me.Text, Me.Font, Brushes.White, rect, sf)
    End Sub
End Class

' Secondary flat button ("Continue as Guest").
Public Class DarkButton
    Inherits Button

    Public Property FillColor As Color = Color.FromArgb(28, 32, 52)
    Public Property BorderColor As Color = Color.FromArgb(50, 56, 82)

    Public Sub New()
        Me.FlatStyle = FlatStyle.Flat
        Me.FlatAppearance.BorderSize = 0
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 10.5, FontStyle.Regular)
        Me.Cursor = Cursors.Hand
        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Using path As GraphicsPath = RoundedRectPath(rect, 10)
            Using fillBrush As New SolidBrush(FillColor)
                g.FillPath(fillBrush, path)
            End Using
            Using pen As New Pen(BorderColor, 1)
                g.DrawPath(pen, path)
            End Using
        End Using
        Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
        g.DrawString(Me.Text, Me.Font, Brushes.White, rect, sf)
    End Sub
End Class

Public Enum LoginOutcome
    Cancelled
    SignedIn
    Guest
End Enum

Public Class LoginForm
    Inherits Form

    Private pnlLeft As Panel
    Private pnlRight As Panel
    Private card As RoundedPanel

    Private txtUser As TextBox
    Private txtPass As TextBox
    Private Const PlaceholderUser As String = "Student ID or email"
    Private Const PlaceholderPass As String = "Password"

    ' Populated once the user signs in / continues as guest; read by Program.vb.
    Public Property Outcome As LoginOutcome = LoginOutcome.Cancelled
    Public Property SignedInName As String = ""
    Public Property SignedInRole As String = "" ' "Admin" or "Student"

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1400, 800)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(6, 8, 18)
        Me.Text = "ChemLab Virtual - Sign in"
        Me.KeyPreview = True

        AddHandler Me.KeyDown, Sub(s, e)
                                    If e.KeyCode = Keys.Escape Then Me.Close()
                                End Sub

        BuildLeftPanel()
        BuildRightPanel()

        AddHandler Me.Resize, Sub(s, e) LayoutUI()
        LayoutUI()
    End Sub

    Private Sub LayoutUI()
        pnlLeft.Width = CInt(Me.ClientSize.Width * 0.55)
        pnlLeft.Height = Me.ClientSize.Height
        pnlRight.Left = pnlLeft.Width
        pnlRight.Width = Me.ClientSize.Width - pnlLeft.Width
        pnlRight.Height = Me.ClientSize.Height

        Dim cardWidth As Integer = 460
        Dim cardHeight As Integer = 620
        card.Width = cardWidth
        card.Height = cardHeight
        card.Left = Math.Max(20, (pnlRight.Width - cardWidth) \ 2)
        card.Top = Math.Max(20, (pnlRight.Height - cardHeight) \ 2)
    End Sub

    ' ===================== LEFT HERO PANEL =====================

    Private Sub BuildLeftPanel()
        pnlLeft = New Panel()
        pnlLeft.BackColor = Color.FromArgb(6, 8, 18)
        Me.Controls.Add(pnlLeft)
        AddHandler pnlLeft.Paint, AddressOf PaintLeftPanel
    End Sub

    Private Sub PaintLeftPanel(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, pnlLeft.Width, pnlLeft.Height)

        Using bg As New LinearGradientBrush(rect, Color.FromArgb(14, 18, 36), Color.FromArgb(5, 7, 16), 60.0F)
            g.FillRectangle(bg, rect)
        End Using

        DrawAbstractGlassware(g, pnlLeft.Width, pnlLeft.Height)
        DrawMolecule(g, pnlLeft.Width * 0.42F, pnlLeft.Height * 0.12F, 1.0F)

        Using titleFont As New Font("Segoe UI", 30, FontStyle.Bold)
            Using subFont As New Font("Segoe UI", 12, FontStyle.Regular)
                Dim marginX As Integer = 48
                Dim titleY As Integer = pnlLeft.Height - 130
                g.DrawString("Experiment safely. Learn faster.", titleFont, Brushes.White, marginX, titleY)
                Using subBrush As New SolidBrush(Color.FromArgb(190, 197, 214))
                    g.DrawString("A fully interactive 3D chemistry laboratory on your desktop.", subFont, subBrush, marginX, titleY + 48)
                End Using
            End Using
        End Using
    End Sub

    ' Faint outline flask/beaker shapes, purely decorative, echoing the splash icon style.
    Private Sub DrawAbstractGlassware(g As Graphics, panelWidth As Integer, panelHeight As Integer)
        Using pen As New Pen(Color.FromArgb(28, 255, 255, 255), 2)
            ' large flask, left edge, partially off-screen
            Dim fx As Single = -40
            Dim fy As Single = panelHeight * 0.30F
            Dim path As New GraphicsPath()
            path.AddLine(fx + 40, fy, fx + 40, fy + 90)
            path.AddLine(fx + 40, fy + 90, fx - 30, fy + 260)
            path.AddLine(fx - 30, fy + 260, fx + 150, fy + 260)
            path.AddLine(fx + 150, fy + 260, fx + 80, fy + 90)
            path.AddLine(fx + 80, fy + 90, fx + 80, fy)
            g.DrawPath(pen, path)

            ' small beaker, lower area
            Dim bx As Single = panelWidth * 0.18F
            Dim by As Single = panelHeight * 0.62F
            g.DrawRectangle(pen, bx, by, 130, 170)
            g.DrawLine(pen, bx, by + 40, bx + 130, by + 40)
        End Using
    End Sub

    Private Sub DrawMolecule(g As Graphics, offsetX As Single, offsetY As Single, scale As Single)
        Dim nodes As New List(Of PointF) From {
            New PointF(40, 170), New PointF(95, 110), New PointF(150, 60),
            New PointF(215, 35), New PointF(290, 75), New PointF(150, 150),
            New PointF(215, 175), New PointF(180, 245), New PointF(245, 275)
        }
        Dim edges As New List(Of (Integer, Integer)) From {
            (0, 1), (1, 2), (2, 3), (3, 4), (1, 5), (5, 6), (6, 7), (7, 8)
        }
        Dim pts As New List(Of PointF)
        For Each n In nodes
            pts.Add(New PointF(offsetX + n.X * scale, offsetY + n.Y * scale))
        Next

        Using linePen As New Pen(Color.FromArgb(90, 150, 150, 165), 3)
            For Each edge In edges
                g.DrawLine(linePen, pts(edge.Item1), pts(edge.Item2))
            Next
        End Using

        Dim r As Single = 15
        For i As Integer = 0 To pts.Count - 1
            Dim p As PointF = pts(i)
            Dim ballRect As New RectangleF(p.X - r, p.Y - r, r * 2, r * 2)
            Using shBrush As New SolidBrush(Color.FromArgb(60, 0, 0, 0))
                g.FillEllipse(shBrush, ballRect.X + 2, ballRect.Y + 3, ballRect.Width, ballRect.Height)
            End Using
            Using gradBrush As New LinearGradientBrush(ballRect, Color.FromArgb(255, 235, 140, 70), Color.FromArgb(255, 165, 60, 15), 45.0F)
                g.FillEllipse(gradBrush, ballRect)
            End Using
        Next
    End Sub

    ' ===================== RIGHT SIGN-IN PANEL =====================

    Private Sub BuildRightPanel()
        pnlRight = New Panel()
        pnlRight.BackColor = Color.FromArgb(10, 13, 28)
        Me.Controls.Add(pnlRight)

        card = New RoundedPanel()
        card.CornerRadius = 18
        card.FillColor = Color.FromArgb(18, 22, 40)
        card.BorderColor = Color.FromArgb(42, 47, 70)
        pnlRight.Controls.Add(card)

        Dim y As Integer = 32

        ' --- logo row ---
        Dim iconBox As New Panel()
        iconBox.Size = New Size(52, 52)
        iconBox.Location = New Point(32, y)
        AddHandler iconBox.Paint, AddressOf PaintLogoIcon
        card.Controls.Add(iconBox)

        Dim lblChem As New Label()
        lblChem.Text = "ChemLab"
        lblChem.Font = New Font("Segoe UI", 15, FontStyle.Bold)
        lblChem.ForeColor = Color.White
        lblChem.AutoSize = True
        lblChem.BackColor = Color.Transparent
        lblChem.Location = New Point(96, y + 2)
        card.Controls.Add(lblChem)

        Dim lblVirtual As New Label()
        lblVirtual.Text = "V I R T U A L"
        lblVirtual.Font = New Font("Segoe UI", 8.5, FontStyle.Regular)
        lblVirtual.ForeColor = Color.FromArgb(150, 158, 185)
        lblVirtual.AutoSize = True
        lblVirtual.BackColor = Color.Transparent
        lblVirtual.Location = New Point(97, y + 28)
        card.Controls.Add(lblVirtual)

        y += 80

        ' --- headings ---
        Dim lblSignIn As New Label()
        lblSignIn.Text = "Sign in"
        lblSignIn.Font = New Font("Segoe UI", 19, FontStyle.Bold)
        lblSignIn.ForeColor = Color.White
        lblSignIn.AutoSize = True
        lblSignIn.BackColor = Color.Transparent
        lblSignIn.Location = New Point(32, y)
        card.Controls.Add(lblSignIn)
        y += 42

        Dim lblSub As New Label()
        lblSub.Text = "Use your institution account."
        lblSub.Font = New Font("Segoe UI", 10.5, FontStyle.Regular)
        lblSub.ForeColor = Color.FromArgb(160, 168, 190)
        lblSub.AutoSize = True
        lblSub.BackColor = Color.Transparent
        lblSub.Location = New Point(32, y)
        card.Controls.Add(lblSub)
        y += 44

        ' --- input fields ---
        txtUser = CreateInputField(card, 32, y, 396, "person", PlaceholderUser, False)
        y += 66
        txtPass = CreateInputField(card, 32, y, 396, "lock", PlaceholderPass, True)
        y += 74

        ' --- buttons ---
        Dim btnEnter As New GradientButton()
        btnEnter.Text = "Enter the Lab"
        btnEnter.Size = New Size(396, 46)
        btnEnter.Location = New Point(32, y)
        AddHandler btnEnter.Click, AddressOf BtnEnter_Click
        card.Controls.Add(btnEnter)
        y += 58

        Dim btnGuest As New DarkButton()
        btnGuest.Text = "Continue as Guest"
        btnGuest.Size = New Size(396, 46)
        btnGuest.Location = New Point(32, y)
        AddHandler btnGuest.Click, AddressOf BtnGuest_Click
        card.Controls.Add(btnGuest)
        y += 58

        ' --- links ---
        Dim lnkForgot As New LinkLabel()
        lnkForgot.Text = "Forgot password?"
        lnkForgot.Font = New Font("Segoe UI", 9.5, FontStyle.Regular)
        lnkForgot.LinkColor = Color.FromArgb(56, 214, 255)
        lnkForgot.ActiveLinkColor = Color.FromArgb(90, 225, 255)
        lnkForgot.BackColor = Color.Transparent
        lnkForgot.AutoSize = True
        lnkForgot.Location = New Point(32, y)
        card.Controls.Add(lnkForgot)

        Dim lnkCreate As New LinkLabel()
        lnkCreate.Text = "Create an account"
        lnkCreate.Font = New Font("Segoe UI", 9.5, FontStyle.Regular)
        lnkCreate.LinkColor = Color.FromArgb(56, 214, 255)
        lnkCreate.ActiveLinkColor = Color.FromArgb(90, 225, 255)
        lnkCreate.BackColor = Color.Transparent
        lnkCreate.AutoSize = True
        card.Controls.Add(lnkCreate)
        lnkCreate.Location = New Point(428 - lnkCreate.Width, y)

        AddHandler lnkForgot.LinkClicked, Sub()
                                               Using frm As New ForgotPasswordForm()
                                                   frm.ShowDialog(Me)
                                               End Using
                                           End Sub

        AddHandler lnkCreate.LinkClicked, Sub()
                                               Using frm As New CreateAccountForm()
                                                   If frm.ShowDialog(Me) = DialogResult.OK Then
                                                       MessageBox.Show("Your account has been created. You can now sign in.", "Welcome to ChemLab Virtual",
                                                                        MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                   End If
                                               End Using
                                           End Sub

        y += 40

        ' --- footer note ---
        Dim lblOffline As New Label()
        lblOffline.Text = "Offline mode available — experiments sync when reconnected."
        lblOffline.Font = New Font("Segoe UI", 9, FontStyle.Regular)
        lblOffline.ForeColor = Color.FromArgb(120, 128, 150)
        lblOffline.BackColor = Color.Transparent
        lblOffline.TextAlign = ContentAlignment.MiddleCenter
        lblOffline.Size = New Size(396, 34)
        lblOffline.Location = New Point(32, y)
        card.Controls.Add(lblOffline)
    End Sub

    Private Sub PaintLogoIcon(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, 51, 51)
        Using path As GraphicsPath = RoundedRectPath(rect, 14)
            Using gradBrush As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
                g.FillPath(gradBrush, path)
            End Using
        End Using

        Dim cx As Single = rect.Width / 2.0F
        Dim cy As Single = rect.Height / 2.0F
        Using flaskPen As New Pen(Color.White, 2.4F)
            flaskPen.LineJoin = LineJoin.Round
            flaskPen.StartCap = LineCap.Round
            flaskPen.EndCap = LineCap.Round
            Dim neckTopL As New PointF(cx - 4, cy - 14)
            Dim neckTopR As New PointF(cx + 4, cy - 14)
            Dim neckBotL As New PointF(cx - 4, cy - 2)
            Dim neckBotR As New PointF(cx + 4, cy - 2)
            Dim bodyL As New PointF(cx - 13, cy + 13)
            Dim bodyR As New PointF(cx + 13, cy + 13)

            g.DrawLine(flaskPen, New PointF(neckTopL.X - 3, neckTopL.Y), New PointF(neckTopR.X + 3, neckTopR.Y))
            g.DrawLine(flaskPen, neckTopL, neckBotL)
            g.DrawLine(flaskPen, neckTopR, neckBotR)
            g.DrawLine(flaskPen, neckBotL, bodyL)
            g.DrawLine(flaskPen, neckBotR, bodyR)
            Dim basePts() As PointF = {bodyL, New PointF(bodyL.X + 3, bodyR.Y + 4), New PointF(bodyR.X - 3, bodyR.Y + 4), bodyR}
            g.DrawLines(flaskPen, basePts)
        End Using
    End Sub

    ' Builds a rounded input container with a small hand-drawn icon and a borderless TextBox inside.
    Private Function CreateInputField(parent As Control, x As Integer, y As Integer, width As Integer,
                                       iconType As String, placeholder As String, isPassword As Boolean) As TextBox
        Dim fieldPanel As New RoundedPanel()
        fieldPanel.CornerRadius = 10
        fieldPanel.FillColor = Color.FromArgb(24, 28, 48)
        fieldPanel.BorderColor = Color.FromArgb(46, 51, 76)
        fieldPanel.Size = New Size(width, 48)
        fieldPanel.Location = New Point(x, y)
        parent.Controls.Add(fieldPanel)

        Dim iconPanel As New Panel()
        iconPanel.Size = New Size(20, 20)
        iconPanel.Location = New Point(14, 14)
        iconPanel.BackColor = Color.Transparent
        AddHandler iconPanel.Paint, Sub(s, e)
                                         If iconType = "person" Then
                                             DrawPersonIcon(e.Graphics)
                                         Else
                                             DrawLockIcon(e.Graphics)
                                         End If
                                     End Sub
        fieldPanel.Controls.Add(iconPanel)

        Dim tb As New TextBox()
        tb.BorderStyle = BorderStyle.None
        tb.BackColor = fieldPanel.FillColor
        tb.Font = New Font("Segoe UI", 11, FontStyle.Regular)
        tb.Location = New Point(46, 14)
        tb.Width = width - 60
        tb.Text = placeholder
        tb.ForeColor = Color.FromArgb(120, 128, 150)
        fieldPanel.Controls.Add(tb)

        AddHandler tb.Enter, Sub(s, e)
                                  If tb.Text = placeholder Then
                                      tb.Text = ""
                                      tb.ForeColor = Color.FromArgb(230, 233, 240)
                                      If isPassword Then tb.UseSystemPasswordChar = True
                                  End If
                              End Sub
        AddHandler tb.Leave, Sub(s, e)
                                  If tb.Text.Length = 0 Then
                                      If isPassword Then tb.UseSystemPasswordChar = False
                                      tb.Text = placeholder
                                      tb.ForeColor = Color.FromArgb(120, 128, 150)
                                  End If
                              End Sub

        Return tb
    End Function

    Private Sub DrawPersonIcon(g As Graphics)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(Color.FromArgb(150, 158, 180), 1.6F)
            g.DrawEllipse(pen, 5, 1, 10, 10)
            g.DrawArc(pen, 1, 11, 18, 14, 180, 180)
        End Using
    End Sub

    Private Sub DrawLockIcon(g As Graphics)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(Color.FromArgb(150, 158, 180), 1.6F)
            g.DrawArc(pen, 4, 0, 12, 12, 180, 180)
            g.DrawRectangle(pen, 2, 9, 16, 11)
        End Using
    End Sub

    ' ===================== ACTIONS =====================

    Private Sub BtnEnter_Click(sender As Object, e As EventArgs)
        Dim userVal As String = If(txtUser.Text = PlaceholderUser, "", txtUser.Text).Trim()
        Dim passVal As String = If(txtPass.Text = PlaceholderPass, "", txtPass.Text)

        If userVal.Length = 0 OrElse passVal.Length = 0 Then
            MessageBox.Show("Please enter your Student ID/email and password.", "Sign in",
                             MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' TODO: replace this with a real call to your authentication/backend service.
        ' This demo treats any email starting with "admin" as an administrator account,
        ' and everyone else as a student, purely so the flow can be tested end-to-end.
        If userVal.ToLower().StartsWith("admin") Then
            SignedInRole = "Admin"
            SignedInName = "Mac Falen"
        Else
            SignedInRole = "Student"
            SignedInName = If(userVal.Contains("@"), CultureInfo_TitleCase(userVal.Split("@"c)(0)), CultureInfo_TitleCase(userVal))
        End If

        Outcome = LoginOutcome.SignedIn
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub BtnGuest_Click(sender As Object, e As EventArgs)
        Outcome = LoginOutcome.Guest
        SignedInName = "Guest"
        SignedInRole = "Guest"
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Function CultureInfo_TitleCase(s As String) As String
        If s.Length = 0 Then Return s
        Return Char.ToUpper(s(0)) & s.Substring(1)
    End Function

End Class
