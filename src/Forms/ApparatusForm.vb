Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

''' <summary>
''' "Apparatus" screen: grid of 3D lab-equipment items the student can drag onto
''' the bench or toggle visibility for. Mirrors the sidebar/title-bar chrome used
''' by HomeForm so the two screens feel like the same app.
''' </summary>
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
    Private titleBar As Panel
    Private content As Panel

    ' name, capacity ("—" for none), status ("On bench" / "Selected" / "Hidden" / "In shelf")
    Private ReadOnly apparatusItems As (String, String, String)() = {
        ("Conical Flask", "250 ml", "On bench"),
        ("Beaker", "500 ml", "On bench"),
        ("Round Flask", "250 ml", "Selected"),
        ("Bunsen Burner", "—", "On bench"),
        ("Molecular Model", "—", "Hidden"),
        ("Clamp Stand", "—", "On bench"),
        ("Burette", "50 ml", "In shelf"),
        ("Test Tube Rack", "6 tubes", "In shelf")
    }

    Public Sub New(Optional displayName As String = "Mac Falen", Optional role As String = "Student")
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
        titleBar = New Panel()
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

        Dim btnClose As New Label()
        btnClose.Text = "✕"
        btnClose.Font = New Font("Segoe UI", 10)
        btnClose.ForeColor = Color.FromArgb(160, 168, 190)
        btnClose.Size = New Size(40, 40)
        btnClose.TextAlign = ContentAlignment.MiddleCenter
        btnClose.Cursor = Cursors.Hand
        btnClose.Dock = DockStyle.Right
        AddHandler btnClose.Click, Sub() Me.Close()
        AddHandler btnClose.MouseEnter, Sub() btnClose.BackColor = Color.FromArgb(200, 60, 60)
        AddHandler btnClose.MouseLeave, Sub() btnClose.BackColor = Color.Transparent

        Dim btnMax As New Label()
        btnMax.Text = "☐"
        btnMax.Font = New Font("Segoe UI", 9)
        btnMax.ForeColor = Color.FromArgb(160, 168, 190)
        btnMax.Size = New Size(40, 40)
        btnMax.TextAlign = ContentAlignment.MiddleCenter
        btnMax.Cursor = Cursors.Hand
        btnMax.Dock = DockStyle.Right
        AddHandler btnMax.Click, Sub()
                                      Me.WindowState = If(Me.WindowState = FormWindowState.Maximized, FormWindowState.Normal, FormWindowState.Maximized)
                                  End Sub
        AddHandler btnMax.MouseEnter, Sub() btnMax.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMax.MouseLeave, Sub() btnMax.BackColor = Color.Transparent

        Dim btnMin As New Label()
        btnMin.Text = "—"
        btnMin.Font = New Font("Segoe UI", 9)
        btnMin.ForeColor = Color.FromArgb(160, 168, 190)
        btnMin.Size = New Size(40, 40)
        btnMin.TextAlign = ContentAlignment.MiddleCenter
        btnMin.Cursor = Cursors.Hand
        btnMin.Dock = DockStyle.Right
        AddHandler btnMin.Click, Sub() Me.WindowState = FormWindowState.Minimized
        AddHandler btnMin.MouseEnter, Sub() btnMin.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMin.MouseLeave, Sub() btnMin.BackColor = Color.Transparent

        titleBar.Controls.Add(btnClose)
        titleBar.Controls.Add(btnMax)
        titleBar.Controls.Add(btnMin)
    End Sub

    ' ===================== SIDEBAR =====================

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

        Dim lblChem As New Label()
        lblChem.Text = "ChemLab"
        lblChem.Font = New Font("Segoe UI", 13, FontStyle.Bold)
        lblChem.ForeColor = Color.White
        lblChem.AutoSize = True
        lblChem.Location = New Point(74, 20)
        sidebar.Controls.Add(lblChem)

        Dim lblVirtual As New Label()
        lblVirtual.Text = "V I R T U A L"
        lblVirtual.Font = New Font("Segoe UI", 7.5)
        lblVirtual.ForeColor = Color.FromArgb(150, 158, 185)
        lblVirtual.AutoSize = True
        lblVirtual.Location = New Point(75, 43)
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

        ' profile footer
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

        Dim lblName As New Label()
        lblName.Text = userName
        lblName.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        lblName.ForeColor = Color.White
        lblName.AutoSize = True
        lblName.Location = New Point(68, 12)
        footer.Controls.Add(lblName)

        Dim lblRole As New Label()
        lblRole.Text = userRole
        lblRole.Font = New Font("Segoe UI", 8.5)
        lblRole.ForeColor = Color.FromArgb(140, 148, 170)
        lblRole.AutoSize = True
        lblRole.Location = New Point(68, 32)
        footer.Controls.Add(lblRole)

        Dim btnBack As New Label()
        btnBack.Text = "⏻"
        btnBack.Font = New Font("Segoe UI", 12)
        btnBack.ForeColor = Color.FromArgb(150, 158, 185)
        btnBack.Size = New Size(30, 30)
        btnBack.TextAlign = ContentAlignment.MiddleCenter
        btnBack.Location = New Point(sidebar.Width - 46, 17)
        btnBack.Cursor = Cursors.Hand
        AddHandler btnBack.Click, Sub()
                                       Dim confirm = MessageBox.Show("Log out of ChemLab Virtual?", "Log out", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                       If confirm = DialogResult.Yes Then
                                           Me.DialogResult = DialogResult.Retry
                                           Me.Close()
                                       End If
                                   End Sub
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

        If isActive Then
            ' Already on the Apparatus screen — nothing to do.
        ElseIf iconKey = "home" Then
            Dim goHome As EventHandler = Sub()
                                              Me.DialogResult = DialogResult.Retry
                                              Me.Close()
                                          End Sub
            AddHandler item.Click, goHome
            AddHandler lbl.Click, goHome
        Else
            Dim handler As EventHandler = Sub() MessageBox.Show($"'{label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
            AddHandler item.Click, handler
            AddHandler lbl.Click, handler
        End If

        If Not isActive Then
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

        Dim lblTitle As New Label()
        lblTitle.Text = "Apparatus"
        lblTitle.Font = New Font("Segoe UI", 22, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(36, 28)
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label()
        lblSub.Text = "Drag any 3D item onto the bench, or toggle its visibility in the scene."
        lblSub.Font = New Font("Segoe UI", 10.5)
        lblSub.ForeColor = Color.FromArgb(140, 148, 210)
        lblSub.AutoSize = True
        lblSub.Location = New Point(36, 62)
        content.Controls.Add(lblSub)

        Dim btnAdd As New GradientButton()
        btnAdd.Text = "+  Add to Bench"
        btnAdd.Size = New Size(160, 40)
        btnAdd.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnAdd.Location = New Point(content.Width - 160 - 36, 30)
        AddHandler btnAdd.Click, Sub() MessageBox.Show("Choose an item below, then use 'Add to Bench' to place it on the workbench.", "ChemLab Virtual")
        content.Controls.Add(btnAdd)

        BuildCardsGrid()
        BuildInfoBanner()
    End Sub

    Private Sub BuildCardsGrid()
        Const cols As Integer = 4
        Const gap As Integer = 20
        Const cardH As Integer = 178
        Dim gridLeft As Integer = 36
        Dim gridTop As Integer = 108
        Dim gridWidth As Integer = Me.ClientSize.Width - sidebar.Width - 72
        Dim cardW As Integer = (gridWidth - gap * (cols - 1)) \ cols

        For i As Integer = 0 To apparatusItems.Length - 1
            Dim col As Integer = i Mod cols
            Dim row As Integer = i \ cols
            Dim x As Integer = gridLeft + col * (cardW + gap)
            Dim y As Integer = gridTop + row * (cardH + gap)
            CreateApparatusCard(apparatusItems(i).Item1, apparatusItems(i).Item2, apparatusItems(i).Item3, x, y, cardW, cardH)
        Next
    End Sub

    Private Sub CreateApparatusCard(name As String, capacity As String, status As String, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim isSelected As Boolean = (status = "Selected")

        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = If(isSelected, Color.FromArgb(108, 92, 231), Color.FromArgb(36, 41, 66))
        card.Location = New Point(x, y)
        card.Size = New Size(w, h)
        content.Controls.Add(card)

        ' 3D-preview tile
        Dim tile As New RoundedPanel()
        tile.CornerRadius = 10
        tile.FillColor = Color.FromArgb(21, 26, 48)
        tile.BorderColor = Color.FromArgb(34, 39, 62)
        tile.Location = New Point(16, 16)
        tile.Size = New Size(w - 32, 92)
        AddHandler tile.Paint, Sub(s, e) DrawGlasswareIcon(e.Graphics, tile.Width, tile.Height)
        card.Controls.Add(tile)

        Dim btnMore As New Label()
        btnMore.Text = "⋮"
        btnMore.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        btnMore.ForeColor = Color.FromArgb(140, 148, 170)
        btnMore.Size = New Size(24, 22)
        btnMore.TextAlign = ContentAlignment.MiddleCenter
        btnMore.Location = New Point(w - 40, 14)
        btnMore.Cursor = Cursors.Hand
        AddHandler btnMore.Click, Sub() MessageBox.Show($"Options for '{name}' are coming soon.", "ChemLab Virtual")
        card.Controls.Add(btnMore)
        btnMore.BringToFront()

        Dim lblName As New Label()
        lblName.Text = name
        lblName.Font = New Font("Segoe UI", 10.5, FontStyle.Bold)
        lblName.ForeColor = Color.White
        lblName.AutoSize = True
        lblName.Location = New Point(16, 116)
        card.Controls.Add(lblName)

        Dim lblCapacity As New Label()
        lblCapacity.Text = "Capacity " & capacity
        lblCapacity.Font = New Font("Segoe UI", 8.5)
        lblCapacity.ForeColor = Color.FromArgb(140, 148, 170)
        lblCapacity.AutoSize = True
        lblCapacity.Location = New Point(16, 136)
        card.Controls.Add(lblCapacity)

        Dim badge As New Label()
        badge.Text = "  " & status & "  "
        badge.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        badge.AutoSize = True
        badge.Location = New Point(16, h - 36)
        If isSelected Then
            badge.BackColor = Color.FromArgb(108, 92, 231)
            badge.ForeColor = Color.White
        ElseIf status = "On bench" Then
            badge.BackColor = Color.FromArgb(20, 60, 46)
            badge.ForeColor = Color.FromArgb(120, 220, 170)
        Else
            badge.BackColor = Color.FromArgb(30, 34, 56)
            badge.ForeColor = Color.FromArgb(150, 158, 180)
        End If
        card.Controls.Add(badge)

        Dim lblDetails As New Label()
        lblDetails.Text = "Details"
        lblDetails.Font = New Font("Segoe UI", 9, FontStyle.Underline)
        lblDetails.ForeColor = Color.FromArgb(150, 130, 240)
        lblDetails.AutoSize = True
        lblDetails.Cursor = Cursors.Hand
        lblDetails.Location = New Point(w - 20 - TextRenderer.MeasureText("Details", lblDetails.Font).Width, h - 34)
        AddHandler lblDetails.Click, Sub() MessageBox.Show($"Details for '{name}' — capacity {capacity}, status ""{status}"".", "ChemLab Virtual")
        card.Controls.Add(lblDetails)
    End Sub

    Private Sub BuildInfoBanner()
        Dim gridWidth As Integer = Me.ClientSize.Width - sidebar.Width - 72
        Dim rows As Integer = CInt(Math.Ceiling(apparatusItems.Length / 4.0))
        Dim bannerY As Integer = 108 + rows * (178 + 20) + 4

        Dim banner As New RoundedPanel()
        banner.CornerRadius = 12
        banner.FillColor = Color.FromArgb(16, 20, 40)
        banner.BorderColor = Color.FromArgb(36, 41, 66)
        banner.Location = New Point(36, bannerY)
        banner.Size = New Size(gridWidth, 56)
        banner.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        content.Controls.Add(banner)

        Dim iconCircle As New Panel()
        iconCircle.Size = New Size(22, 22)
        iconCircle.Location = New Point(18, 17)
        AddHandler iconCircle.Paint, Sub(s, e)
                                          Dim g = e.Graphics
                                          g.SmoothingMode = SmoothingMode.AntiAlias
                                          Using pen As New Pen(Color.FromArgb(120, 130, 220), 1.6F)
                                              g.DrawEllipse(pen, 1, 1, 19, 19)
                                          End Using
                                          Using f As New Font("Segoe UI", 9, FontStyle.Bold)
                                              g.DrawString("i", f, New SolidBrush(Color.FromArgb(120, 130, 220)), 8, 3)
                                          End Using
                                      End Sub
        banner.Controls.Add(iconCircle)

        Dim lblInfo As New Label()
        lblInfo.Text = "Each apparatus maps to a 3D model with attachment points. Selecting an item highlights its docking sockets on the bench so it can be clamped, heated or filled."
        lblInfo.Font = New Font("Segoe UI", 9)
        lblInfo.ForeColor = Color.FromArgb(160, 168, 190)
        lblInfo.Location = New Point(52, 18)
        lblInfo.Size = New Size(gridWidth - 70, 34)
        lblInfo.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        banner.Controls.Add(lblInfo)
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
        DrawNavIcon(g, "flask", Color.White)
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

    ''' <summary>Simple two-test-tube glyph, centered in the given tile size, used for every card preview.</summary>
    Private Sub DrawGlasswareIcon(g As Graphics, tileW As Integer, tileH As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim accent As Color = Color.FromArgb(94, 234, 212)
        Dim cx As Single = tileW / 2.0F
        Dim cy As Single = tileH / 2.0F
        Dim tubeW As Single = 14
        Dim tubeH As Single = 44
        Dim gap As Single = 10

        Using pen As New Pen(accent, 2.4F) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
            For Each dx In New Single() {-(tubeW + gap) / 2, (tubeW + gap) / 2}
                Dim topY As Single = cy - tubeH / 2
                Dim botY As Single = cy + tubeH / 2
                Dim leftX As Single = cx + dx - tubeW / 2
                Dim rightX As Single = cx + dx + tubeW / 2
                g.DrawLine(pen, leftX, topY, leftX, botY - 6)
                g.DrawLine(pen, rightX, topY, rightX, botY - 6)
                Dim path As New GraphicsPath()
                path.AddArc(leftX, botY - 12, tubeW, 12, 0, 180)
                g.DrawPath(pen, path)
                g.DrawLine(pen, leftX - 3, topY, rightX + 3, topY)
            Next
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