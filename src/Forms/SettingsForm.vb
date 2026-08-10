Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' Settings screen: sidebar + titlebar (same chrome as HomeForm/ReportsGrades) plus a
' 2x2 grid of setting cards (Account, Simulation & 3D, Audio, Notifications) with
' pill-style values and clickable On/Off toggles — matching the "Settings" screenshot.
Public Class Settings
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

    ' ----- data -----

    Private Class SettingRow
        Public Property Label As String
        Public Property IsToggle As Boolean
        Public Property Value As String          ' static display value (non-toggle rows)
        Public Property ValuePanel As RoundedPanel
        Public Property ValueLabel As Label
    End Class

    Private Class SettingCard
        Public Property Title As String
        Public Property Icon As String
        Public Property AccentColor As Color
        Public Property Rows As New List(Of SettingRow)
    End Class

    Private ReadOnly toggleStates As New Dictionary(Of String, Boolean) From {
        {"Show apparatus labels", True},
        {"Reaction sounds", True},
        {"Interface clicks", False},
        {"Quiz reminders", True},
        {"Report deadlines", True}
    }

    Private ReadOnly cardAccount As New SettingCard With {
        .Title = "Account", .Icon = "👤", .AccentColor = Color.FromArgb(130, 180, 255),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Display name", .Value = "Mac Falen"},
            New SettingRow With {.Label = "Role", .Value = "Student"},
            New SettingRow With {.Label = "Institution", .Value = "Riverside College"}
        }
    }

    Private ReadOnly cardSimulation As New SettingCard With {
        .Title = "Simulation & 3D", .Icon = "🖥", .AccentColor = Color.FromArgb(100, 220, 210),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Render quality", .Value = "High"},
            New SettingRow With {.Label = "Anti-aliasing", .Value = "Enabled"},
            New SettingRow With {.Label = "Camera sensitivity", .Value = "Medium"},
            New SettingRow With {.Label = "Show apparatus labels", .IsToggle = True}
        }
    }

    Private ReadOnly cardAudio As New SettingCard With {
        .Title = "Audio", .Icon = "🔊", .AccentColor = Color.FromArgb(180, 150, 255),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Reaction sounds", .IsToggle = True},
            New SettingRow With {.Label = "Interface clicks", .IsToggle = True},
            New SettingRow With {.Label = "Master volume", .Value = "70%"}
        }
    }

    Private ReadOnly cardNotifications As New SettingCard With {
        .Title = "Notifications", .Icon = "🔔", .AccentColor = Color.FromArgb(230, 170, 100),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Quiz reminders", .IsToggle = True},
            New SettingRow With {.Label = "Report deadlines", .IsToggle = True}
        }
    }

    Public Sub New(displayName As String, role As String)
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1500, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Settings"

        ' reflect the signed-in user's display name into the Account card
        cardAccount.Rows(0).Value = userName
        cardAccount.Rows(1).Value = userRole

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

        Dim lblTitle As New Label() With {.Text = "ChemLab Virtual — Settings", .Font = New Font("Segoe UI", 9.5, FontStyle.Regular),
                                           .ForeColor = Color.FromArgb(150, 158, 185), .AutoSize = True, .Location = New Point(16, 11)}
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

    ' ===================== SIDEBAR (same nav as HomeForm/ReportsGrades, "Settings" active) =====================

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
            ("grid", "Apparatus", False),
            ("beaker", "Chemicals", False),
            ("notebook", "Lab Notebook", False),
            ("question", "Quizzes", False),
            ("chart", "Reports && Grades", False),
            ("shield", "Safety Data", False),
            ("cap", "Teacher Dashboard", False),
            ("gear", "Settings", True)
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
        AddHandler btnBack.Click, Sub() Me.Close() ' closes this screen, returns to whichever screen opened it
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

        Dim lblTitle As New Label() With {.Text = "Settings", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {.Text = "Application preferences, rendering quality and accessibility.",
                                         .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblSub)

        ' ----- top-right action: Save Changes -----
        Dim btnSave As New GradientButton() With {.Text = "Save Changes", .Size = New Size(150, 36)}
        btnSave.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnSave.Location = New Point(content.Width - 36 - btnSave.Width, 30)
        AddHandler btnSave.Click, Sub() MessageBox.Show("Your settings have been saved (demo).", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
        content.Controls.Add(btnSave)

        ' ----- 2x2 card grid -----
        Dim gridY As Integer = 104
        Dim colGap As Integer = 20
        Dim rowGap As Integer = 20
        Dim colWidth As Integer = (content.Width - 72 - colGap) \ 2

        Dim topRowHeight As Integer = Math.Max(CardHeight(cardAccount), CardHeight(cardSimulation))
        Dim bottomRowHeight As Integer = Math.Max(CardHeight(cardAudio), CardHeight(cardNotifications))

        BuildSettingsCard(cardAccount, 36, gridY, colWidth, topRowHeight)
        BuildSettingsCard(cardSimulation, 36 + colWidth + colGap, gridY, colWidth, topRowHeight)

        Dim bottomY As Integer = gridY + topRowHeight + rowGap
        BuildSettingsCard(cardAudio, 36, bottomY, colWidth, bottomRowHeight)
        BuildSettingsCard(cardNotifications, 36 + colWidth + colGap, bottomY, colWidth, bottomRowHeight)

        BuildBottomToolbar()

        AddHandler content.Resize, Sub() btnSave.Location = New Point(content.Width - 36 - btnSave.Width, 30)
    End Sub

    ' ----- card height helper: header + one row per setting + padding -----
    Private Function CardHeight(card As SettingCard) As Integer
        Return 58 + card.Rows.Count * 46 + 18
    End Function

    ' ----- setting card (header + rows of label/value pill) -----

    Private Sub BuildSettingsCard(card As SettingCard, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblIcon As New Label() With {.Text = card.Icon, .Font = New Font("Segoe UI Emoji", 11), .ForeColor = card.AccentColor,
                                          .AutoSize = True, .Location = New Point(18, 18), .BackColor = Color.Transparent}
        panel.Controls.Add(lblIcon)

        Dim lblTitle As New Label() With {.Text = card.Title, .Font = New Font("Segoe UI", 11.5, FontStyle.Bold), .ForeColor = Color.White,
                                           .AutoSize = True, .Location = New Point(46, 17), .BackColor = Color.Transparent}
        panel.Controls.Add(lblTitle)

        Dim rowY As Integer = 58
        For Each row In card.Rows
            Dim lblLabel As New Label() With {.Text = row.Label, .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(150, 158, 205),
                                               .AutoSize = True, .Location = New Point(18, rowY + 4), .BackColor = Color.Transparent}
            panel.Controls.Add(lblLabel)

            Dim displayText As String = If(row.IsToggle, If(toggleStates(row.Label), "On", "Off"), row.Value)

            Dim pillWidth As Integer = TextRenderer.MeasureText(displayText, New Font("Segoe UI", 8.5, FontStyle.Bold)).Width + 26
            pillWidth = Math.Max(pillWidth, 44)

            Dim pill As New RoundedPanel()
            pill.CornerRadius = 10
            pill.Size = New Size(pillWidth, 24)
            pill.Location = New Point(w - 18 - pillWidth, rowY)
            pill.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            pill.Cursor = Cursors.Hand

            Dim lblValue As New Label() With {.Text = displayText, .Font = New Font("Segoe UI", 8.5, FontStyle.Bold),
                                               .AutoSize = False, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter,
                                               .BackColor = Color.Transparent}
            pill.Controls.Add(lblValue)

            row.ValuePanel = pill
            row.ValueLabel = lblValue
            StylePill(row)

            If row.IsToggle Then
                Dim toggleHandler As EventHandler = Sub()
                                                          toggleStates(row.Label) = Not toggleStates(row.Label)
                                                          row.ValueLabel.Text = If(toggleStates(row.Label), "On", "Off")
                                                          StylePill(row)
                                                      End Sub
                AddHandler pill.Click, toggleHandler
                AddHandler lblValue.Click, toggleHandler
            Else
                Dim infoHandler As EventHandler = Sub() MessageBox.Show($"Editing '{row.Label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
                AddHandler pill.Click, infoHandler
                AddHandler lblValue.Click, infoHandler
            End If

            panel.Controls.Add(pill)
            rowY += 46
        Next
    End Sub

    Private Sub StylePill(row As SettingRow)
        If row.IsToggle AndAlso toggleStates(row.Label) Then
            row.ValuePanel.FillColor = Color.FromArgb(44, 38, 92)
            row.ValuePanel.BorderColor = Color.FromArgb(108, 92, 231)
            row.ValueLabel.ForeColor = Color.FromArgb(190, 180, 255)
        ElseIf row.IsToggle Then
            row.ValuePanel.FillColor = Color.FromArgb(24, 28, 46)
            row.ValuePanel.BorderColor = Color.FromArgb(40, 45, 68)
            row.ValueLabel.ForeColor = Color.FromArgb(140, 148, 170)
        Else
            row.ValuePanel.FillColor = Color.FromArgb(24, 28, 46)
            row.ValuePanel.BorderColor = Color.FromArgb(40, 45, 68)
            row.ValueLabel.ForeColor = Color.FromArgb(225, 228, 245)
        End If
        row.ValuePanel.Invalidate()
    End Sub

    ' ----- floating bottom toolbar -----

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
                                        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 60)
                                    End Sub
        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 60)
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

End ClassImports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' Settings screen: sidebar + titlebar (same chrome as HomeForm/ReportsGrades) plus a
' 2x2 grid of setting cards (Account, Simulation & 3D, Audio, Notifications) with
' pill-style values and clickable On/Off toggles — matching the "Settings" screenshot.
Public Class Settings
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

    ' ----- data -----

    Private Class SettingRow
        Public Property Label As String
        Public Property IsToggle As Boolean
        Public Property Value As String          ' static display value (non-toggle rows)
        Public Property ValuePanel As RoundedPanel
        Public Property ValueLabel As Label
    End Class

    Private Class SettingCard
        Public Property Title As String
        Public Property Icon As String
        Public Property AccentColor As Color
        Public Property Rows As New List(Of SettingRow)
    End Class

    Private ReadOnly toggleStates As New Dictionary(Of String, Boolean) From {
        {"Show apparatus labels", True},
        {"Reaction sounds", True},
        {"Interface clicks", False},
        {"Quiz reminders", True},
        {"Report deadlines", True}
    }

    Private ReadOnly cardAccount As New SettingCard With {
        .Title = "Account", .Icon = "👤", .AccentColor = Color.FromArgb(130, 180, 255),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Display name", .Value = "Mac Falen"},
            New SettingRow With {.Label = "Role", .Value = "Student"},
            New SettingRow With {.Label = "Institution", .Value = "Riverside College"}
        }
    }

    Private ReadOnly cardSimulation As New SettingCard With {
        .Title = "Simulation & 3D", .Icon = "🖥", .AccentColor = Color.FromArgb(100, 220, 210),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Render quality", .Value = "High"},
            New SettingRow With {.Label = "Anti-aliasing", .Value = "Enabled"},
            New SettingRow With {.Label = "Camera sensitivity", .Value = "Medium"},
            New SettingRow With {.Label = "Show apparatus labels", .IsToggle = True}
        }
    }

    Private ReadOnly cardAudio As New SettingCard With {
        .Title = "Audio", .Icon = "🔊", .AccentColor = Color.FromArgb(180, 150, 255),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Reaction sounds", .IsToggle = True},
            New SettingRow With {.Label = "Interface clicks", .IsToggle = True},
            New SettingRow With {.Label = "Master volume", .Value = "70%"}
        }
    }

    Private ReadOnly cardNotifications As New SettingCard With {
        .Title = "Notifications", .Icon = "🔔", .AccentColor = Color.FromArgb(230, 170, 100),
        .Rows = New List(Of SettingRow) From {
            New SettingRow With {.Label = "Quiz reminders", .IsToggle = True},
            New SettingRow With {.Label = "Report deadlines", .IsToggle = True}
        }
    }

    Public Sub New(displayName As String, role As String)
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1500, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Settings"

        ' reflect the signed-in user's display name into the Account card
        cardAccount.Rows(0).Value = userName
        cardAccount.Rows(1).Value = userRole

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

        Dim lblTitle As New Label() With {.Text = "ChemLab Virtual — Settings", .Font = New Font("Segoe UI", 9.5, FontStyle.Regular),
                                           .ForeColor = Color.FromArgb(150, 158, 185), .AutoSize = True, .Location = New Point(16, 11)}
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

    ' ===================== SIDEBAR (same nav as HomeForm/ReportsGrades, "Settings" active) =====================

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
            ("grid", "Apparatus", False),
            ("beaker", "Chemicals", False),
            ("notebook", "Lab Notebook", False),
            ("question", "Quizzes", False),
            ("chart", "Reports && Grades", False),
            ("shield", "Safety Data", False),
            ("cap", "Teacher Dashboard", False),
            ("gear", "Settings", True)
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
        AddHandler btnBack.Click, Sub() Me.Close() ' closes this screen, returns to whichever screen opened it
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

        Dim lblTitle As New Label() With {.Text = "Settings", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {.Text = "Application preferences, rendering quality and accessibility.",
                                         .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblSub)

        ' ----- top-right action: Save Changes -----
        Dim btnSave As New GradientButton() With {.Text = "Save Changes", .Size = New Size(150, 36)}
        btnSave.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnSave.Location = New Point(content.Width - 36 - btnSave.Width, 30)
        AddHandler btnSave.Click, Sub() MessageBox.Show("Your settings have been saved (demo).", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
        content.Controls.Add(btnSave)

        ' ----- 2x2 card grid -----
        Dim gridY As Integer = 104
        Dim colGap As Integer = 20
        Dim rowGap As Integer = 20
        Dim colWidth As Integer = (content.Width - 72 - colGap) \ 2

        Dim topRowHeight As Integer = Math.Max(CardHeight(cardAccount), CardHeight(cardSimulation))
        Dim bottomRowHeight As Integer = Math.Max(CardHeight(cardAudio), CardHeight(cardNotifications))

        BuildSettingsCard(cardAccount, 36, gridY, colWidth, topRowHeight)
        BuildSettingsCard(cardSimulation, 36 + colWidth + colGap, gridY, colWidth, topRowHeight)

        Dim bottomY As Integer = gridY + topRowHeight + rowGap
        BuildSettingsCard(cardAudio, 36, bottomY, colWidth, bottomRowHeight)
        BuildSettingsCard(cardNotifications, 36 + colWidth + colGap, bottomY, colWidth, bottomRowHeight)

        BuildBottomToolbar()

        AddHandler content.Resize, Sub() btnSave.Location = New Point(content.Width - 36 - btnSave.Width, 30)
    End Sub

    ' ----- card height helper: header + one row per setting + padding -----
    Private Function CardHeight(card As SettingCard) As Integer
        Return 58 + card.Rows.Count * 46 + 18
    End Function

    ' ----- setting card (header + rows of label/value pill) -----

    Private Sub BuildSettingsCard(card As SettingCard, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblIcon As New Label() With {.Text = card.Icon, .Font = New Font("Segoe UI Emoji", 11), .ForeColor = card.AccentColor,
                                          .AutoSize = True, .Location = New Point(18, 18), .BackColor = Color.Transparent}
        panel.Controls.Add(lblIcon)

        Dim lblTitle As New Label() With {.Text = card.Title, .Font = New Font("Segoe UI", 11.5, FontStyle.Bold), .ForeColor = Color.White,
                                           .AutoSize = True, .Location = New Point(46, 17), .BackColor = Color.Transparent}
        panel.Controls.Add(lblTitle)

        Dim rowY As Integer = 58
        For Each row In card.Rows
            Dim lblLabel As New Label() With {.Text = row.Label, .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(150, 158, 205),
                                               .AutoSize = True, .Location = New Point(18, rowY + 4), .BackColor = Color.Transparent}
            panel.Controls.Add(lblLabel)

            Dim displayText As String = If(row.IsToggle, If(toggleStates(row.Label), "On", "Off"), row.Value)

            Dim pillWidth As Integer = TextRenderer.MeasureText(displayText, New Font("Segoe UI", 8.5, FontStyle.Bold)).Width + 26
            pillWidth = Math.Max(pillWidth, 44)

            Dim pill As New RoundedPanel()
            pill.CornerRadius = 10
            pill.Size = New Size(pillWidth, 24)
            pill.Location = New Point(w - 18 - pillWidth, rowY)
            pill.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            pill.Cursor = Cursors.Hand

            Dim lblValue As New Label() With {.Text = displayText, .Font = New Font("Segoe UI", 8.5, FontStyle.Bold),
                                               .AutoSize = False, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter,
                                               .BackColor = Color.Transparent}
            pill.Controls.Add(lblValue)

            row.ValuePanel = pill
            row.ValueLabel = lblValue
            StylePill(row)

            If row.IsToggle Then
                Dim toggleHandler As EventHandler = Sub()
                                                          toggleStates(row.Label) = Not toggleStates(row.Label)
                                                          row.ValueLabel.Text = If(toggleStates(row.Label), "On", "Off")
                                                          StylePill(row)
                                                      End Sub
                AddHandler pill.Click, toggleHandler
                AddHandler lblValue.Click, toggleHandler
            Else
                Dim infoHandler As EventHandler = Sub() MessageBox.Show($"Editing '{row.Label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
                AddHandler pill.Click, infoHandler
                AddHandler lblValue.Click, infoHandler
            End If

            panel.Controls.Add(pill)
            rowY += 46
        Next
    End Sub

    Private Sub StylePill(row As SettingRow)
        If row.IsToggle AndAlso toggleStates(row.Label) Then
            row.ValuePanel.FillColor = Color.FromArgb(44, 38, 92)
            row.ValuePanel.BorderColor = Color.FromArgb(108, 92, 231)
            row.ValueLabel.ForeColor = Color.FromArgb(190, 180, 255)
        ElseIf row.IsToggle Then
            row.ValuePanel.FillColor = Color.FromArgb(24, 28, 46)
            row.ValuePanel.BorderColor = Color.FromArgb(40, 45, 68)
            row.ValueLabel.ForeColor = Color.FromArgb(140, 148, 170)
        Else
            row.ValuePanel.FillColor = Color.FromArgb(24, 28, 46)
            row.ValuePanel.BorderColor = Color.FromArgb(40, 45, 68)
            row.ValueLabel.ForeColor = Color.FromArgb(225, 228, 245)
        End If
        row.ValuePanel.Invalidate()
    End Sub

    ' ----- floating bottom toolbar -----

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
                                        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 60)
                                    End Sub
        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 60)
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

End Classv