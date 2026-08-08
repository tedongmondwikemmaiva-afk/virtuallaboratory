Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' Apparatus library screen: sidebar + titlebar (same chrome as HomeForm/AdminDashboardForm)
' plus a card grid of 3D apparatus items with status badges, an info banner, and a
' floating bottom toolbar, matching the "Apparatus" screenshot.
Public Class ApparatusForm
    Inherits Form

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    Private ReadOnly userName As String
    Private ReadOnly userRole As String

    Private sidebar As Panel
    Private content As Panel

    ' name, capacity, status ("On bench" / "Selected" / "Hidden" / "In shelf")
    Private ReadOnly items As (String, String, String)() = {
        ("Conical Flask", "Capacity 250 ml", "On bench"),
        ("Beaker", "Capacity 500 ml", "On bench"),
        ("Round Flask", "Capacity 250 ml", "Selected"),
        ("Bunsen Burner", "Capacity —", "On bench"),
        ("Molecular Model", "Capacity —", "Hidden"),
        ("Clamp Stand", "Capacity —", "On bench"),
        ("Burette", "Capacity 50 ml", "In shelf"),
        ("Test Tube Rack", "Capacity 6 tubes", "In shelf")
    }

    Public Sub New(displayName As String, role As String)
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1500, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Apparatus"

        BuildTitleBar()
        BuildSidebar()
        BuildContent()
    End Sub

    ' ===================== TITLE BAR =====================

    Private Sub BuildTitleBar()
        Dim titleBar As New Panel()
        titleBar.Dock = DockStyle.Top
        titleBar.Height = 40
        titleBar.BackColor = Color.FromArgb(9, 12, 24)
        Me.Controls.Add(titleBar)

        Dim lblTitle As New Label()
        lblTitle.Text = "ChemLab Virtual — Apparatus"
        lblTitle.Font = New Font("Segoe UI", 9.5, FontStyle.Regular)
        lblTitle.ForeColor = Color.FromArgb(150, 158, 185)
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(16, 11)
        titleBar.Controls.Add(lblTitle)

        AddHandler titleBar.MouseDown, Sub(s, e)
                                            If e.Button = MouseButtons.Left Then
                                                ReleaseCapture()
                                                SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
                                            End If
                                        End Sub

        Dim btnClose As New Label() With {.Text = "✕", .Font = New Font("Segoe UI", 10), .Size = New Size(40, 40),
                                           .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.FromArgb(160, 168, 190),
                                           .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnClose.Click, Sub() Me.Close()
        AddHandler btnClose.MouseEnter, Sub() btnClose.BackColor = Color.FromArgb(200, 60, 60)
        AddHandler btnClose.MouseLeave, Sub() btnClose.BackColor = Color.Transparent

        Dim btnMax As New Label() With {.Text = "☐", .Font = New Font("Segoe UI", 9), .Size = New Size(40, 40),
                                         .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.FromArgb(160, 168, 190),
                                         .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnMax.Click, Sub() Me.WindowState = If(Me.WindowState = FormWindowState.Maximized, FormWindowState.Normal, FormWindowState.Maximized)
        AddHandler btnMax.MouseEnter, Sub() btnMax.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMax.MouseLeave, Sub() btnMax.BackColor = Color.Transparent

        Dim btnMin As New Label() With {.Text = "—", .Font = New Font("Segoe UI", 9), .Size = New Size(40, 40),
                                         .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.FromArgb(160, 168, 190),
                                         .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnMin.Click, Sub() Me.WindowState = FormWindowState.Minimized
        AddHandler btnMin.MouseEnter, Sub() btnMin.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMin.MouseLeave, Sub() btnMin.BackColor = Color.Transparent

        titleBar.Controls.Add(btnClose)
        titleBar.Controls.Add(btnMax)
        titleBar.Controls.Add(btnMin)
    End Sub

    ' ===================== SIDEBAR (same nav as HomeForm, "Apparatus" active) =====================

    Private Sub BuildSidebar()
        sidebar = New Panel()
        sidebar.Dock = DockStyle.Left
        sidebar.Width = 244
        sidebar.BackColor = Color.FromArgb(12, 15, 30)
        Me.Controls.Add(sidebar)

        Dim iconBox As New Panel()
        iconBox.Size = New Size(44, 44)
        iconBox.Location = New Point(20, 20)
        AddHandler iconBox.Paint, AddressOf PaintLogoIcon
        sidebar.Controls.Add(iconBox)

        Dim lblChem As New Label() With {.Text = "ChemLab", .Font = New Font("Segoe UI", 13, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(74, 20)}
        Dim lblVirtual As New Label() With {.Text = "V I R T U A L", .Font = New Font("Segoe UI", 7.5), .ForeColor = Color.FromArgb(150, 158, 185), .AutoSize = True, .Location = New Point(75, 43)}
        sidebar.Controls.Add(lblChem)
        sidebar.Controls.Add(lblVirtual)

        Dim navItems As (String, String, Boolean)() = {
            ("home", "Home", False),
            ("flask", "Lab Workspace", False),
            ("book", "Experiments", False),
            ("grid", "Apparatus", True),
            ("beaker", "Chemicals", False),
            ("notebook", "Lab Notebook", False),
            ("question", "Quizzes", False),
            ("chart", "Reports && Grades", False),
            ("shield", "Safety Data", False),
            ("cap", "Teacher Dashboard", False),
            ("gear", "Settings", False)
        }

        Dim y As Integer = 90
        For Each item In navItems
            CreateNavItem(item.Item1, item.Item2.Replace("&&", "&"), item.Item3, y)
            y += 46
        Next

        Dim footer As New Panel()
        footer.Size = New Size(sidebar.Width, 64)
        footer.Location = New Point(0, sidebar.Height - 64)
        footer.Anchor = AnchorStyles.Left Or AnchorStyles.Bottom Or AnchorStyles.Right
        footer.BackColor = Color.FromArgb(14, 17, 34)
        sidebar.Controls.Add(footer)

        Dim avatar As New Panel()
        avatar.Size = New Size(38, 38)
        avatar.Location = New Point(20, 13)
        Dim initials As String = GetInitials(userName)
        AddHandler avatar.Paint, Sub(s, e) PaintAvatar(e.Graphics, avatar.Width, avatar.Height, initials)
        footer.Controls.Add(avatar)

        Dim lblName As New Label() With {.Text = userName, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(68, 12)}
        Dim lblRole As New Label() With {.Text = userRole, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(140, 148, 170), .AutoSize = True, .Location = New Point(68, 32)}
        footer.Controls.Add(lblName)
        footer.Controls.Add(lblRole)

        Dim btnBack As New Label() With {.Text = "⏻", .Font = New Font("Segoe UI", 12), .ForeColor = Color.FromArgb(150, 158, 185),
                                          .Size = New Size(30, 30), .TextAlign = ContentAlignment.MiddleCenter,
                                          .Location = New Point(sidebar.Width - 46, 17), .Cursor = Cursors.Hand}
        AddHandler btnBack.Click, Sub() Me.Close() ' closes this dialog, returns to whichever screen opened it
        footer.Controls.Add(btnBack)
    End Sub

    Private Sub CreateNavItem(iconKey As String, label As String, isActive As Boolean, y As Integer)
        Dim item As New RoundedPanel()
        item.CornerRadius = 10
        item.Size = New Size(sidebar.Width - 32, 40)
        item.Location = New Point(16, y)
        item.Cursor = Cursors.Hand
        If isActive Then
            item.FillColor = Color.FromArgb(108, 92, 231)
            item.BorderColor = Color.FromArgb(108, 92, 231)
        Else
            item.FillColor = sidebar.BackColor
            item.BorderColor = sidebar.BackColor
        End If

        Dim iconPanel As New Panel()
        iconPanel.Size = New Size(18, 18)
        iconPanel.Location = New Point(14, 11)
        AddHandler iconPanel.Paint, Sub(s, e) DrawNavIcon(e.Graphics, iconKey, Color.White)
        item.Controls.Add(iconPanel)

        Dim lbl As New Label()
        lbl.Text = label
        lbl.Font = New Font("Segoe UI", 10, If(isActive, FontStyle.Bold, FontStyle.Regular))
        lbl.ForeColor = If(isActive, Color.White, Color.FromArgb(180, 188, 208))
        lbl.AutoSize = True
        lbl.Location = New Point(42, 10)
        item.Controls.Add(lbl)

        If Not isActive Then
            Dim handler As EventHandler = Sub()
                                               If iconKey = "home" Then
                                                   Me.DialogResult = DialogResult.Cancel ' just close, no logout
                                                   Me.Close()
                                               Else
                                                   MessageBox.Show($"'{label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                               End If
                                           End Sub
            AddHandler item.Click, handler
            AddHandler lbl.Click, handler
            AddHandler item.MouseEnter, Sub()
                                             item.FillColor = Color.FromArgb(22, 26, 46)
                                             item.Invalidate()
                                         End Sub
            AddHandler item.MouseLeave, Sub()
                                             item.FillColor = sidebar.BackColor
                                             item.Invalidate()
                                         End Sub
        End If

        sidebar.Controls.Add(item)
    End Sub

    Private Function GetInitials(fullName As String) As String
        Dim parts = fullName.Split(" "c)
        If parts.Length >= 2 Then Return (parts(0).Substring(0, 1) & parts(1).Substring(0, 1)).ToUpper()
        If fullName.Length >= 2 Then Return fullName.Substring(0, 2).ToUpper()
        Return fullName.ToUpper()
    End Function

    ' ===================== MAIN CONTENT =====================

    Private Sub BuildContent()
        content = New Panel()
        content.Dock = DockStyle.Fill
        content.BackColor = Color.FromArgb(9, 12, 24)
        content.AutoScroll = True
        Me.Controls.Add(content)
        Me.Controls.SetChildIndex(content, 0)

        Dim lblTitle As New Label() With {.Text = "Apparatus", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {.Text = "Drag any 3D item onto the bench, or toggle its visibility in the scene.",
                                          .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblSub)

        Dim btnAdd As New GradientButton() With {.Text = "+  Add to Bench", .Size = New Size(160, 40)}
        btnAdd.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnAdd.Location = New Point(content.Width - 36 - btnAdd.Width, 28)
        AddHandler btnAdd.Click, Sub() MessageBox.Show("Add-to-bench flow coming soon.", "ChemLab Virtual")
        content.Controls.Add(btnAdd)

        ' ----- card grid: 4 columns -----
        Const cols As Integer = 4
        Dim gap As Integer = 20
        Dim gridTop As Integer = 108
        Dim cardW As Integer = (content.Width - 72 - gap * (cols - 1)) \ cols
        Dim cardH As Integer = 176

        For i As Integer = 0 To items.Length - 1
            Dim row As Integer = i \ cols
            Dim col As Integer = i Mod cols
            Dim cx As Integer = 36 + col * (cardW + gap)
            Dim cy As Integer = gridTop + row * (cardH + gap)
            BuildApparatusCard(items(i).Item1, items(i).Item2, items(i).Item3, cx, cy, cardW, cardH)
        Next

        Dim gridRows As Integer = CInt(Math.Ceiling(items.Length / CDbl(cols)))
        Dim gridBottom As Integer = gridTop + gridRows * cardH + (gridRows - 1) * gap

        ' ----- info banner -----
        BuildInfoBanner(36, gridBottom + 24, content.Width - 72)

        ' ----- floating bottom toolbar -----
        BuildBottomToolbar()
    End Sub

    Private Sub BuildApparatusCard(itemName As String, capacity As String, status As String, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim isSelected As Boolean = (status = "Selected")

        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = If(isSelected, Color.FromArgb(108, 92, 231), Color.FromArgb(36, 41, 66))
        card.Location = New Point(x, y)
        card.Size = New Size(w, h)
        card.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        content.Controls.Add(card)

        ' preview / icon area
        Dim preview As New RoundedPanel()
        preview.CornerRadius = 10
        preview.FillColor = Color.FromArgb(22, 26, 48)
        preview.BorderColor = Color.FromArgb(34, 39, 64)
        preview.Location = New Point(10, 10)
        preview.Size = New Size(w - 20, 90)
        card.Controls.Add(preview)
        AddHandler preview.Paint, Sub(s, e)
                                       Select Case itemName
                                           Case "Beaker"
                                               DrawBeakerIcon(e.Graphics, preview.Width, preview.Height)
                                           Case "Conical Flask", "Round Flask"
                                               DrawFlaskIcon(e.Graphics, preview.Width, preview.Height)
                                           Case "Bunsen Burner"
                                               DrawBunsenIcon(e.Graphics, preview.Width, preview.Height)
                                           Case Else
                                               DrawTestTubesIcon(e.Graphics, preview.Width, preview.Height)
                                       End Select
                                   End Sub

        Dim lblInfo As New Label() With {.Text = "ⓘ", .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(150, 158, 180),
                                          .AutoSize = True, .BackColor = Color.Transparent}
        lblInfo.Location = New Point(w - 30, 16)
        card.Controls.Add(lblInfo)

        Dim lblName As New Label() With {.Text = itemName, .Font = New Font("Segoe UI", 10.5, FontStyle.Bold), .ForeColor = Color.White,
                                          .AutoSize = True, .Location = New Point(10, 108)}
        card.Controls.Add(lblName)

        Dim lblCap As New Label() With {.Text = capacity, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(140, 148, 170),
                                         .AutoSize = True, .Location = New Point(10, 126)}
        card.Controls.Add(lblCap)

        ' status badge
        Dim badgeColors As (Color, Color) = StatusColors(status)
        Dim badge As New Label() With {.Text = status, .Font = New Font("Segoe UI", 8, FontStyle.Bold), .AutoSize = True,
                                        .ForeColor = badgeColors.Item1, .BackColor = badgeColors.Item2,
                                        .Padding = New Padding(8, 3, 8, 3), .Location = New Point(10, h - 32)}
        card.Controls.Add(badge)

        Dim lnkDetails As New LinkLabel() With {.Text = "Details", .Font = New Font("Segoe UI", 8.5, FontStyle.Bold),
                                                 .LinkColor = Color.FromArgb(56, 214, 255), .ActiveLinkColor = Color.FromArgb(90, 225, 255),
                                                 .AutoSize = True}
        lnkDetails.Location = New Point(w - 20 - lnkDetails.PreferredWidth, h - 30)
        AddHandler lnkDetails.LinkClicked, Sub() MessageBox.Show($"{itemName} — {capacity} ({status})", "Apparatus details")
        card.Controls.Add(lnkDetails)
    End Sub

    Private Sub DrawBeakerIcon(g As Graphics, w As Integer, h As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim cx As Single = w / 2.0F
        Dim cy As Single = h / 2.0F
        Using pen As New Pen(Color.FromArgb(80, 220, 210), 2.4F)
            pen.LineJoin = LineJoin.Round
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            ' simple trapezoid beaker
            Dim topL As New PointF(cx - 28, cy - 20)
            Dim topR As New PointF(cx + 28, cy - 20)
            Dim botL As New PointF(cx - 18, cy + 22)
            Dim botR As New PointF(cx + 18, cy + 22)
            g.DrawLine(pen, topL, topR)
            g.DrawLine(pen, topL, botL)
            g.DrawLine(pen, topR, botR)
            g.DrawLine(pen, botL, botR)
            Using fillBrush As New SolidBrush(Color.FromArgb(60, 80, 220, 210))
                g.FillPolygon(fillBrush, {topL, topR, botR, botL})
            End Using
        End Using
    End Sub

    Private Sub DrawFlaskIcon(g As Graphics, w As Integer, h As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim cx As Single = w / 2.0F
        Dim cy As Single = h / 2.0F
        Using pen As New Pen(Color.FromArgb(220, 220, 220), 2.4F)
            pen.LineJoin = LineJoin.Round
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            Dim neckTop As New PointF(cx, cy - 26)
            Dim neckLeft As New PointF(cx - 6, cy - 6)
            Dim neckRight As New PointF(cx + 6, cy - 6)
            Dim bodyLeft As New PointF(cx - 18, cy + 18)
            Dim bodyRight As New PointF(cx + 18, cy + 18)
            g.DrawLine(pen, New PointF(neckTop.X - 6, neckTop.Y), New PointF(neckTop.X + 6, neckTop.Y))
            g.DrawLine(pen, New PointF(neckTop.X - 4, neckTop.Y), neckLeft)
            g.DrawLine(pen, New PointF(neckTop.X + 4, neckTop.Y), neckRight)
            g.DrawLine(pen, neckLeft, bodyLeft)
            g.DrawLine(pen, neckRight, bodyRight)
            Dim basePts() As PointF = {bodyLeft, New PointF(bodyLeft.X + 6, bodyLeft.Y + 6), New PointF(bodyRight.X - 6, bodyRight.Y + 6), bodyRight}
            g.DrawLines(pen, basePts)
            Using fillBrush As New SolidBrush(Color.FromArgb(60, 120, 180, 240))
                g.FillEllipse(fillBrush, cx - 12, cy, 24, 18)
            End Using
        End Using
    End Sub

    Private Sub DrawBunsenIcon(g As Graphics, w As Integer, h As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim cx As Single = w / 2.0F
        Dim cy As Single = h / 2.0F
        Using pen As New Pen(Color.FromArgb(200, 180, 140), 2.0F)
            pen.LineJoin = LineJoin.Round
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            ' flame
            Using flame As New SolidBrush(Color.FromArgb(220, 140, 60))
                g.FillEllipse(flame, cx - 8, cy - 30, 16, 28)
            End Using
            ' burner body
            g.DrawRectangle(pen, cx - 6, cy - 4, 12, 20)
            g.DrawLine(pen, cx - 12, cy + 18, cx + 12, cy + 18)
        End Using
    End Sub

    Private Function StatusColors(status As String) As (Color, Color)
        Select Case status
            Case "On bench"
                Return (Color.FromArgb(120, 220, 170), Color.FromArgb(20, 46, 38))
            Case "Selected"
                Return (Color.White, Color.FromArgb(108, 92, 231))
            Case "Hidden"
                Return (Color.FromArgb(150, 158, 180), Color.FromArgb(30, 34, 54))
            Case "In shelf"
                Return (Color.FromArgb(140, 170, 220), Color.FromArgb(22, 34, 54))
            Case Else
                Return (Color.FromArgb(150, 158, 180), Color.FromArgb(30, 34, 54))
        End Select
    End Function

    Private Sub DrawTestTubesIcon(g As Graphics, w As Integer, h As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim cx As Single = w / 2.0F
        Dim cy As Single = h / 2.0F
        Using pen As New Pen(Color.FromArgb(80, 220, 210), 2.4F)
            pen.LineJoin = LineJoin.Round
            pen.StartCap = LineCap.Round
            pen.EndCap = LineCap.Round
            For Each dx As Single In {-9.0F, 9.0F}
                Dim topL As New PointF(cx + dx - 5, cy - 22)
                Dim topR As New PointF(cx + dx + 5, cy - 22)
                Dim botL As New PointF(cx + dx - 5, cy + 14)
                Dim botR As New PointF(cx + dx + 5, cy + 14)
                g.DrawLine(pen, topL, botL)
                g.DrawLine(pen, topR, botR)
                g.DrawArc(pen, botL.X, botL.Y - 5, 10, 10, 0, 180)
                Using fillBrush As New SolidBrush(Color.FromArgb(70, 80, 220, 210))
                    g.FillPie(fillBrush, botL.X, botL.Y - 10, 10, 20, 0, 180)
                    g.FillRectangle(fillBrush, botL.X, botL.Y - 6, 10, 6)
                End Using
            Next
        End Using
    End Sub

    Private Sub BuildInfoBanner(x As Integer, y As Integer, w As Integer)
        Dim banner As New RoundedPanel()
        banner.CornerRadius = 12
        banner.FillColor = Color.FromArgb(15, 19, 36)
        banner.BorderColor = Color.FromArgb(34, 39, 64)
        banner.Location = New Point(x, y)
        banner.Size = New Size(w, 54)
        banner.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        content.Controls.Add(banner)

        Dim lblIcon As New Label() With {.Text = "ⓘ", .Font = New Font("Segoe UI", 11), .ForeColor = Color.FromArgb(120, 170, 231),
                                          .AutoSize = True, .Location = New Point(18, 16)}
        banner.Controls.Add(lblIcon)

        Dim lblText As New Label() With {
            .Text = "Each apparatus maps to a 3D model with attachment points. Selecting an item highlights its docking sockets on the bench so it can be clamped, heated or filled.",
            .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(160, 168, 190),
            .Location = New Point(42, 10), .Size = New Size(w - 60, 34)}
        banner.Controls.Add(lblText)
    End Sub

    Private Sub BuildBottomToolbar()
        Dim toolbar As New RoundedPanel()
        toolbar.CornerRadius = 20
        toolbar.FillColor = Color.FromArgb(18, 22, 40)
        toolbar.BorderColor = Color.FromArgb(40, 45, 70)
        toolbar.Size = New Size(180, 40)
        toolbar.Anchor = AnchorStyles.Bottom
        content.Controls.Add(toolbar)
        toolbar.BringToFront()

        Dim icons As String() = {"⟲", "T", "✎", "💬"}
        Dim ix As Integer = 16
        For Each ic In icons
            Dim lbl As New Label() With {.Text = ic, .Font = New Font("Segoe UI", 10), .ForeColor = Color.FromArgb(180, 188, 208),
                                          .AutoSize = True, .Location = New Point(ix, 10), .Cursor = Cursors.Hand}
            toolbar.Controls.Add(lbl)
            ix += 38
        Next

        AddHandler content.Resize, Sub()
                                        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 70)
                                    End Sub
        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 70)
    End Sub

    ' ===================== SHARED DRAWING =====================

    Private Sub PaintLogoIcon(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, 43, 43)
        Using path = RoundedRectPath(rect, 12)
            Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
                g.FillPath(br, path)
            End Using
        End Using
        Dim cx As Single = rect.Width / 2.0F
        Dim cy As Single = rect.Height / 2.0F
        Using flaskPen As New Pen(Color.White, 2.0F)
            flaskPen.LineJoin = LineJoin.Round
            flaskPen.StartCap = LineCap.Round
            flaskPen.EndCap = LineCap.Round
            Dim neckTopL As New PointF(cx - 3, cy - 12)
            Dim neckTopR As New PointF(cx + 3, cy - 12)
            Dim neckBotL As New PointF(cx - 3, cy - 1)
            Dim neckBotR As New PointF(cx + 3, cy - 1)
            Dim bodyL As New PointF(cx - 11, cy + 11)
            Dim bodyR As New PointF(cx + 11, cy + 11)
            g.DrawLine(flaskPen, New PointF(neckTopL.X - 2, neckTopL.Y), New PointF(neckTopR.X + 2, neckTopR.Y))
            g.DrawLine(flaskPen, neckTopL, neckBotL)
            g.DrawLine(flaskPen, neckTopR, neckBotR)
            g.DrawLine(flaskPen, neckBotL, bodyL)
            g.DrawLine(flaskPen, neckBotR, bodyR)
            Dim basePts() As PointF = {bodyL, New PointF(bodyL.X + 3, bodyR.Y + 3), New PointF(bodyR.X - 3, bodyR.Y + 3), bodyR}
            g.DrawLines(flaskPen, basePts)
        End Using
    End Sub

    Private Sub PaintAvatar(g As Graphics, w As Integer, h As Integer, initials As String)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, w - 1, h - 1)
        Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
            g.FillEllipse(br, rect)
        End Using
        Using f As New Font("Segoe UI", 9, FontStyle.Bold)
            Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            g.DrawString(initials, f, Brushes.White, rect, sf)
        End Using
    End Sub

    Private Sub DrawNavIcon(g As Graphics, key As String, color As Color)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(color, 1.6F)
            Select Case key
                Case "home"
                    g.DrawLines(pen, {New PointF(1, 9), New PointF(9, 1), New PointF(17, 9)})
                    g.DrawRectangle(pen, 3, 9, 12, 8)
                Case "flask", "beaker"
                    g.DrawLine(pen, 6, 1, 6, 7) : g.DrawLine(pen, 12, 1, 12, 7)
                    g.DrawLine(pen, 6, 7, 2, 17) : g.DrawLine(pen, 12, 7, 16, 17)
                    g.DrawLine(pen, 2, 17, 16, 17)
                Case "book", "notebook"
                    g.DrawRectangle(pen, 2, 2, 14, 14)
                    g.DrawLine(pen, 2, 7, 16, 7)
                Case "grid"
                    g.DrawRectangle(pen, 2, 2, 6, 6) : g.DrawRectangle(pen, 10, 2, 6, 6)
                    g.DrawRectangle(pen, 2, 10, 6, 6) : g.DrawRectangle(pen, 10, 10, 6, 6)
                Case "question"
                    g.DrawEllipse(pen, 1, 1, 16, 16)
                    Using f As New Font("Segoe UI", 8, FontStyle.Bold)
                        g.DrawString("?", f, New SolidBrush(color), 5, 2)
                    End Using
                Case "chart"
                    g.DrawLine(pen, 2, 16, 2, 8) : g.DrawLine(pen, 8, 16, 8, 3) : g.DrawLine(pen, 14, 16, 14, 10)
                Case "shield"
                    g.DrawLines(pen, {New PointF(9, 1), New PointF(17, 4), New PointF(17, 9), New PointF(9, 17), New PointF(1, 9), New PointF(1, 4), New PointF(9, 1)})
                Case "cap"
                    g.DrawLines(pen, {New PointF(1, 6), New PointF(9, 1), New PointF(17, 6), New PointF(9, 11), New PointF(1, 6)})
                    g.DrawLine(pen, 4, 8, 4, 13)
                Case "gear"
                    g.DrawEllipse(pen, 4, 4, 10, 10)
                    g.DrawEllipse(pen, 7, 7, 4, 4)
                Case Else
                    g.DrawEllipse(pen, 2, 2, 14, 14)
            End Select
        End Using
    End Sub

End Class