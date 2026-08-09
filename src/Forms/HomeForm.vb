Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class HomeForm
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

    Public Sub New(displayName As String, role As String)
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1500, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Home"

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
        lblTitle.Text = "ChemLab Virtual — Home"
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

        ' logo
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
            ("home", "Home", True),
            ("flask", "Lab Workspace", False),
            ("book", "Experiments", False),
            ("grid", "Apparatus", False),
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

        Dim btnLogout As New Label()
        btnLogout.Text = "⏻"
        btnLogout.Font = New Font("Segoe UI", 12)
        btnLogout.ForeColor = Color.FromArgb(150, 158, 185)
        btnLogout.Size = New Size(30, 30)
        btnLogout.TextAlign = ContentAlignment.MiddleCenter
        btnLogout.Location = New Point(sidebar.Width - 46, 17)
        btnLogout.Cursor = Cursors.Hand
        AddHandler btnLogout.Click, AddressOf Logout_Click
        footer.Controls.Add(btnLogout)
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
            If label = "Apparatus" Then
                Dim openHandler As EventHandler = Sub()
                                                     Using frm As New ApparatusForm(userName, userRole)
                                                         frm.ShowDialog(Me)
                                                     End Using
                                                 End Sub
                AddHandler item.Click, openHandler
                AddHandler lbl.Click, openHandler
            ElseIf label = "Quizzes" Then
                Dim openHandler As EventHandler = Sub()
                                                     Using frm As New Quizzes(userName, userRole)
                                                         frm.ShowDialog(Me)
                                                     End Using
                                                 End Sub
                AddHandler item.Click, openHandler
                AddHandler lbl.Click, openHandler
            Else
                Dim handler As EventHandler = Sub() MessageBox.Show($"'{label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
                AddHandler item.Click, handler
                AddHandler lbl.Click, handler
            End If
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

    Private Sub Logout_Click(sender As Object, e As EventArgs)
        Dim confirm = MessageBox.Show("Log out of ChemLab Virtual?", "Log out", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If confirm = DialogResult.Yes Then
            Me.DialogResult = DialogResult.Retry ' signals Program.vb to return to the login screen
            Me.Close()
        End If
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

        Dim lblWelcome As New Label()
        lblWelcome.Text = $"Welcome back, {userName.Split(" "c)(0)}"
        lblWelcome.Font = New Font("Segoe UI", 22, FontStyle.Bold)
        lblWelcome.ForeColor = Color.White
        lblWelcome.AutoSize = True
        lblWelcome.Location = New Point(36, 28)
        content.Controls.Add(lblWelcome)

        Dim lblSub As New Label()
        lblSub.Text = "Pick up where you left off in the virtual lab."
        lblSub.Font = New Font("Segoe UI", 10.5)
        lblSub.ForeColor = Color.FromArgb(140, 148, 210)
        lblSub.AutoSize = True
        lblSub.Location = New Point(36, 62)
        content.Controls.Add(lblSub)

        ' banner
        Dim banner As New RoundedPanel()
        banner.CornerRadius = 16
        banner.FillColor = Color.FromArgb(16, 20, 40)
        banner.BorderColor = Color.FromArgb(38, 43, 68)
        banner.Location = New Point(36, 104)
        banner.Size = New Size(content.Width - 72, 250)
        banner.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        AddHandler banner.Paint, AddressOf PaintBannerArt
        content.Controls.Add(banner)

        Dim pill As New Label()
        pill.Text = "  Continue experiment  "
        pill.Font = New Font("Segoe UI", 8.5)
        pill.ForeColor = Color.FromArgb(120, 220, 255)
        pill.BackColor = Color.FromArgb(40, 60, 90)
        pill.AutoSize = True
        pill.Location = New Point(32, 30)
        banner.Controls.Add(pill)

        Dim lblExpTitle As New Label()
        lblExpTitle.Text = "Acid & Base Reaction"
        lblExpTitle.Font = New Font("Segoe UI", 21, FontStyle.Bold)
        lblExpTitle.ForeColor = Color.White
        lblExpTitle.BackColor = Color.Transparent
        lblExpTitle.AutoSize = True
        lblExpTitle.Location = New Point(30, 60)
        banner.Controls.Add(lblExpTitle)

        Dim lblExpDesc As New Label()
        lblExpDesc.Text = "Step 3 of 4 — mix the solutions and record the temperature change."
        lblExpDesc.Font = New Font("Segoe UI", 10)
        lblExpDesc.ForeColor = Color.FromArgb(170, 178, 200)
        lblExpDesc.BackColor = Color.Transparent
        lblExpDesc.AutoSize = True
        lblExpDesc.Location = New Point(32, 100)
        banner.Controls.Add(lblExpDesc)

        Dim btnEnterLab As New GradientButton()
        btnEnterLab.Text = "▶  Enter Lab"
        btnEnterLab.Size = New Size(150, 44)
        btnEnterLab.Location = New Point(32, 140)
        AddHandler btnEnterLab.Click, Sub() MessageBox.Show("Opening the Lab Workspace (coming soon).", "ChemLab Virtual")
        banner.Controls.Add(btnEnterLab)

        Dim btnTheory As New DarkButton()
        btnTheory.Text = "📖  Read Theory"
        btnTheory.Size = New Size(160, 44)
        btnTheory.Location = New Point(192, 140)
        AddHandler btnTheory.Click, Sub() MessageBox.Show("Opening theory notes (coming soon).", "ChemLab Virtual")
        banner.Controls.Add(btnTheory)

        ' stat cards
        Dim stats As (String, String, String, Color, Color)() = {
            ("flask", "18", "Experiments completed", Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205)),
            ("clock", "24.5", "Lab hours logged", Color.FromArgb(92, 130, 231), Color.FromArgb(120, 200, 231)),
            ("badge", "86%", "Quiz average", Color.FromArgb(150, 92, 231), Color.FromArgb(214, 82, 170)),
            ("trend", "7 days", "Current streak", Color.FromArgb(92, 150, 231), Color.FromArgb(180, 92, 231))
        }
        Dim statY As Integer = 372
        Dim statGap As Integer = 20
        Dim statWidth As Integer = (content.Width - 72 - statGap * 3) \ 4
        For i As Integer = 0 To stats.Length - 1
            CreateStatCard(stats(i).Item1, stats(i).Item2, stats(i).Item3, stats(i).Item4, stats(i).Item5,
                           36 + i * (statWidth + statGap), statY, statWidth)
        Next

        ' bottom row: chart + recent experiments
        Dim bottomY As Integer = 486
        Dim rightWidth As Integer = 430
        Dim leftWidth As Integer = content.Width - 72 - rightWidth - 20

        BuildChartPanel(36, bottomY, leftWidth, 300)
        BuildRecentPanel(36 + leftWidth + 20, bottomY, rightWidth, 300)
    End Sub

    Private Sub CreateStatCard(iconKey As String, value As String, label As String, c1 As Color, c2 As Color, x As Integer, y As Integer, w As Integer)
        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = Color.FromArgb(36, 41, 66)
        card.Location = New Point(x, y)
        card.Size = New Size(w, 88)
        card.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        content.Controls.Add(card)

        Dim iconBox As New Panel()
        iconBox.Size = New Size(46, 46)
        iconBox.Location = New Point(18, 21)
        AddHandler iconBox.Paint, Sub(s, e)
                                       Dim g = e.Graphics
                                       g.SmoothingMode = SmoothingMode.AntiAlias
                                       Dim rect As New Rectangle(0, 0, 45, 45)
                                       Using path = RoundedRectPath(rect, 23)
                                           Using br As New LinearGradientBrush(rect, c1, c2, 45.0F)
                                               g.FillPath(br, path)
                                           End Using
                                       End Using
                                       DrawStatIcon(g, iconKey, New RectangleF(12, 12, 21, 21))
                                   End Sub
        card.Controls.Add(iconBox)

        Dim lblValue As New Label()
        lblValue.Text = value
        lblValue.Font = New Font("Segoe UI", 15, FontStyle.Bold)
        lblValue.ForeColor = Color.White
        lblValue.AutoSize = True
        lblValue.Location = New Point(76, 16)
        card.Controls.Add(lblValue)

        Dim lblLabel As New Label()
        lblLabel.Text = label
        lblLabel.Font = New Font("Segoe UI", 9)
        lblLabel.ForeColor = Color.FromArgb(150, 158, 180)
        lblLabel.AutoSize = True
        lblLabel.Location = New Point(76, 44)
        card.Controls.Add(lblLabel)
    End Sub

    Private Sub BuildChartPanel(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblTitle As New Label()
        lblTitle.Text = "📈  Lab activity this week"
        lblTitle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(20, 18)
        panel.Controls.Add(lblTitle)

        Dim chartArea As New Panel()
        chartArea.Location = New Point(20, 56)
        chartArea.Size = New Size(w - 40, h - 76)
        AddHandler chartArea.Paint, AddressOf PaintChart
        panel.Controls.Add(chartArea)
    End Sub

    Private Sub PaintChart(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim area = DirectCast(sender, Panel)
        Dim values() As Double = {2, 3, 1.5, 4, 2, 5, 2}
        Dim days() As String = {"Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"}
        Dim maxVal As Double = 8

        Dim leftMargin As Integer = 26
        Dim bottomMargin As Integer = 24
        Dim chartH As Integer = area.Height - bottomMargin
        Dim chartW As Integer = area.Width - leftMargin

        Using axisPen As New Pen(Color.FromArgb(150, 158, 180))
            Using f As New Font("Segoe UI", 8)
                For i As Integer = 0 To 4
                    Dim v As Integer = i * 2
                    Dim yy As Single = chartH - (v / maxVal) * chartH
                    g.DrawString(v.ToString(), f, Brushes.Gray, 0, yy - 7)
                Next
            End Using
        End Using

        Dim barGap As Integer = 14
        Dim barWidth As Single = (chartW - barGap * (values.Length + 1)) / values.Length
        For i As Integer = 0 To values.Length - 1
            Dim barH As Single = CSng((values(i) / maxVal) * chartH)
            Dim bx As Single = leftMargin + barGap + i * (barWidth + barGap)
            Dim by As Single = chartH - barH
            Dim rect As New RectangleF(bx, by, barWidth, barH)
            Using path = RoundedRectPath(New Rectangle(CInt(rect.X), CInt(rect.Y), CInt(rect.Width), CInt(rect.Height)), 6)
                Using br As New SolidBrush(Color.FromArgb(108, 92, 231))
                    g.FillPath(br, path)
                End Using
            End Using
            Using f As New Font("Segoe UI", 8)
                Dim sz = g.MeasureString(days(i), f)
                g.DrawString(days(i), f, Brushes.Gray, bx + barWidth / 2 - sz.Width / 2, chartH + 6)
            End Using
        Next
    End Sub

    Private Sub BuildRecentPanel(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblTitle As New Label()
        lblTitle.Text = "🧪  Recent experiments"
        lblTitle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(20, 18)
        panel.Controls.Add(lblTitle)

        Dim items As (String, String, String)() = {
            ("Acid & Base Reaction", "Observe neutralization reaction", "100%"),
            ("Precipitation Reaction", "Formation of insoluble precipitate", "60%"),
            ("Gas Evolution", "Reaction producing a gas", "25%"),
            ("Titration", "Find concentration using titration", "0%")
        }

        Dim rowY As Integer = 58
        For Each it In items
            Dim iconBox As New Panel()
            iconBox.Size = New Size(36, 36)
            iconBox.Location = New Point(20, rowY)
            AddHandler iconBox.Paint, Sub(s, e)
                                           Dim g = e.Graphics
                                           g.SmoothingMode = SmoothingMode.AntiAlias
                                           Using path = RoundedRectPath(New Rectangle(0, 0, 35, 35), 10)
                                               Using br As New SolidBrush(Color.FromArgb(30, 34, 56))
                                                   g.FillPath(br, path)
                                               End Using
                                           End Using
                                           DrawStatIcon(g, "flask", New RectangleF(9, 8, 18, 18))
                                       End Sub
            panel.Controls.Add(iconBox)

            Dim lblT As New Label()
            lblT.Text = it.Item1
            lblT.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            lblT.ForeColor = Color.White
            lblT.AutoSize = True
            lblT.Location = New Point(66, rowY)
            panel.Controls.Add(lblT)

            Dim lblD As New Label()
            lblD.Text = it.Item2
            lblD.Font = New Font("Segoe UI", 8.5)
            lblD.ForeColor = Color.FromArgb(140, 148, 170)
            lblD.AutoSize = True
            lblD.Location = New Point(66, rowY + 19)
            panel.Controls.Add(lblD)

            Dim badge As New Label()
            badge.Text = it.Item3
            badge.Font = New Font("Segoe UI", 8.5, FontStyle.Bold)
            badge.ForeColor = If(it.Item3 = "0%", Color.FromArgb(200, 120, 120), Color.FromArgb(120, 220, 170))
            badge.AutoSize = True
            badge.Location = New Point(w - 60, rowY + 8)
            panel.Controls.Add(badge)

            rowY += 58
        Next
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
        DrawStatIcon(g, "flask", New RectangleF(11, 10, 21, 21))
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

    Private Sub PaintBannerArt(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim bannerCtrl = DirectCast(sender, Control)
        Dim w = bannerCtrl.Width
        Dim h = bannerCtrl.Height
        Using pen As New Pen(Color.FromArgb(22, 255, 255, 255), 2)
            g.DrawEllipse(pen, w - 260, -40, 220, 220)
            g.DrawEllipse(pen, w - 140, 60, 140, 140)
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

    Private Sub DrawStatIcon(g As Graphics, key As String, rect As RectangleF)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(Color.White, 1.8F)
            Select Case key
                Case "flask"
                    Dim cx = rect.X + rect.Width / 2 : Dim cy = rect.Y + rect.Height / 2
                    g.DrawLine(pen, cx - 3, rect.Y, cx - 3, cy - 2)
                    g.DrawLine(pen, cx + 3, rect.Y, cx + 3, cy - 2)
                    g.DrawLine(pen, cx - 3, cy - 2, cx - 9, rect.Bottom)
                    g.DrawLine(pen, cx + 3, cy - 2, cx + 9, rect.Bottom)
                    g.DrawLine(pen, cx - 9, rect.Bottom, cx + 9, rect.Bottom)
                Case "clock"
                    g.DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height)
                    Dim cx = rect.X + rect.Width / 2 : Dim cy = rect.Y + rect.Height / 2
                    g.DrawLine(pen, cx, cy, cx, rect.Y + 4)
                    g.DrawLine(pen, cx, cy, rect.Right - 5, cy + 2)
                Case "badge"
                    g.DrawEllipse(pen, rect.X + 2, rect.Y, rect.Width - 4, rect.Width - 4)
                    g.DrawLine(pen, rect.X + 6, rect.Bottom - 6, rect.X + 3, rect.Bottom)
                    g.DrawLine(pen, rect.Right - 6, rect.Bottom - 6, rect.Right - 3, rect.Bottom)
                Case "trend"
                    g.DrawLines(pen, {New PointF(rect.X, rect.Bottom), New PointF(rect.X + rect.Width * 0.35F, rect.Y + rect.Height * 0.4F),
                                      New PointF(rect.X + rect.Width * 0.6F, rect.Y + rect.Height * 0.6F), New PointF(rect.Right, rect.Y)})
            End Select
        End Using
    End Sub

End Class