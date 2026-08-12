Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

''' <summary>
''' "Chemicals" screen: reagent shelf listing name, formula, concentration and
''' hazard class for every stocked chemical, with a "Pour into…" action per row.
''' Mirrors the sidebar/title-bar chrome used by HomeForm/ApparatusForm so every
''' screen feels like the same app.
''' </summary>
Public Class ChemicalsForm
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
    Private tableHeaderLbl As Label

    ' name, formula, concentration, hazard label, dot color, hazard badge color
    Private ReadOnly reagents As (String, String, String, String, Color)() = {
        ("Hydrochloric Acid", "HCl", "1.0 M", "Corrosive", Color.FromArgb(230, 80, 70)),
        ("Sodium Hydroxide", "NaOH", "1.0 M", "Corrosive", Color.FromArgb(64, 200, 210)),
        ("Copper Sulphate", "CuSO" & ChrW(8324), "0.5 M", "Irritant", Color.FromArgb(70, 140, 230)),
        ("Silver Nitrate", "AgNO" & ChrW(8323), "0.1 M", "Oxidiser", Color.FromArgb(210, 214, 224)),
        ("Phenolphthalein", "C" & ChrW(8322) & ChrW(8320) & "H" & ChrW(8321) & ChrW(8324) & "O" & ChrW(8324), "Indicator", "Flammable", Color.FromArgb(232, 90, 160)),
        ("Calcium Carbonate", "CaCO" & ChrW(8323), "Solid", "Low risk", Color.FromArgb(224, 226, 232))
    }

    Public Sub New(Optional displayName As String = "Mac Falen", Optional role As String = "Student")
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1500, 900)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Chemicals"

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
        lblTitle.Text = "ChemLab Virtual — Chemicals"
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
            ("grid", "Apparatus", False),
            ("beaker", "Chemicals", True),
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
            ' Already on the Chemicals screen — nothing to do.
        ElseIf iconKey = "home" Then
            Dim goHome As EventHandler = Sub()
                                              Me.DialogResult = DialogResult.Retry
                                              Me.Close()
                                          End Sub
            AddHandler item.Click, goHome
            AddHandler lbl.Click, goHome
        ElseIf iconKey = "cap" Then
            Dim openTeacher As EventHandler = Sub()
                                                 Try
                                                     Using tf As New TeacherDashboardForm(userName, userRole)
                                                         tf.StartPosition = FormStartPosition.CenterParent
                                                         tf.ShowDialog()
                                                     End Using
                                                 Catch ex As Exception
                                                     MessageBox.Show($"Failed to open Teacher Dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                 End Try
                                             End Sub
            AddHandler item.Click, openTeacher
            AddHandler lbl.Click, openTeacher
        ElseIf iconKey = "chart" Then
            Dim openReports As EventHandler = Sub()
                                                 Try
                                                     Using rf As New ReportsGrades(userName, userRole)
                                                         rf.StartPosition = FormStartPosition.CenterParent
                                                         rf.ShowDialog()
                                                     End Using
                                                 Catch ex As Exception
                                                     MessageBox.Show($"Failed to open Reports & Grades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                 End Try
                                             End Sub
            AddHandler item.Click, openReports
            AddHandler lbl.Click, openReports
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
        lblTitle.Text = "Chemicals"
        lblTitle.Font = New Font("Segoe UI", 22, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(36, 28)
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label()
        lblSub.Text = "Reagent shelf with concentrations, hazard classes and safety data."
        lblSub.Font = New Font("Segoe UI", 10.5)
        lblSub.ForeColor = Color.FromArgb(140, 148, 210)
        lblSub.AutoSize = True
        lblSub.Location = New Point(36, 62)
        content.Controls.Add(lblSub)

        Dim btnSafety As New RoundedPanel()
        btnSafety.CornerRadius = 9
        btnSafety.FillColor = Color.FromArgb(22, 26, 46)
        btnSafety.BorderColor = Color.FromArgb(40, 45, 70)
        btnSafety.Size = New Size(134, 38)
        btnSafety.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnSafety.Location = New Point(content.Width - 134 - 36, 30)
        btnSafety.Cursor = Cursors.Hand
        content.Controls.Add(btnSafety)

        Dim shieldIcon As New Panel()
        shieldIcon.Size = New Size(16, 16)
        shieldIcon.Location = New Point(16, 11)
        AddHandler shieldIcon.Paint, Sub(s, e) DrawNavIcon(e.Graphics, "shield", Color.FromArgb(190, 196, 216))
        btnSafety.Controls.Add(shieldIcon)

        Dim lblSafety As New Label()
        lblSafety.Text = "Safety Guide"
        lblSafety.Font = New Font("Segoe UI", 9.5, FontStyle.Bold)
        lblSafety.ForeColor = Color.FromArgb(210, 214, 230)
        lblSafety.AutoSize = True
        lblSafety.Location = New Point(38, 9)
        btnSafety.Controls.Add(lblSafety)

        Dim safetyHandler As EventHandler = Sub() MessageBox.Show("Safety Guide — hazard pictograms, PPE requirements and first-aid notes for every reagent on the shelf.", "ChemLab Virtual")
        AddHandler btnSafety.Click, safetyHandler
        AddHandler lblSafety.Click, safetyHandler
        AddHandler shieldIcon.Click, safetyHandler

        BuildSearchBox()
        BuildReagentTable()
        BuildInfoBanner()
    End Sub

    Private Sub BuildSearchBox()
        Dim panelWidth As Integer = Me.ClientSize.Width - sidebar.Width - 72

        Dim search As New RoundedPanel()
        search.CornerRadius = 10
        search.FillColor = Color.FromArgb(16, 20, 40)
        search.BorderColor = Color.FromArgb(36, 41, 66)
        search.Location = New Point(36, 84)
        search.Size = New Size(panelWidth, 44)
        search.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        content.Controls.Add(search)

        Dim glassIcon As New Panel()
        glassIcon.Size = New Size(16, 16)
        glassIcon.Location = New Point(16, 14)
        AddHandler glassIcon.Paint, Sub(s, e)
                                         Dim g = e.Graphics
                                         g.SmoothingMode = SmoothingMode.AntiAlias
                                         Using pen As New Pen(Color.FromArgb(130, 138, 165), 1.6F)
                                             g.DrawEllipse(pen, 1, 1, 10, 10)
                                             g.DrawLine(pen, 10, 10, 15, 15)
                                         End Using
                                     End Sub
        search.Controls.Add(glassIcon)

        Dim txt As New TextBox()
        txt.BorderStyle = BorderStyle.None
        txt.BackColor = Color.FromArgb(16, 20, 40)
        txt.ForeColor = Color.FromArgb(210, 214, 230)
        txt.Font = New Font("Segoe UI", 10)
        txt.Text = "Search reagents by name or formula…"
        txt.ForeColor = Color.FromArgb(120, 128, 155)
        txt.Location = New Point(42, 13)
        txt.Width = panelWidth - 60
        txt.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        AddHandler txt.GotFocus, Sub()
                                      If txt.Text = "Search reagents by name or formula…" Then
                                          txt.Text = ""
                                          txt.ForeColor = Color.FromArgb(210, 214, 230)
                                      End If
                                  End Sub
        AddHandler txt.LostFocus, Sub()
                                       If txt.Text = "" Then
                                           txt.Text = "Search reagents by name or formula…"
                                           txt.ForeColor = Color.FromArgb(120, 128, 155)
                                       End If
                                   End Sub
        AddHandler txt.TextChanged, Sub() FilterReagents(If(txt.ForeColor = Color.FromArgb(120, 128, 155), "", txt.Text))
        search.Controls.Add(txt)
    End Sub

    Private Sub FilterReagents(query As String)
        Dim q As String = query.Trim().ToLowerInvariant()
        For Each row As Control In content.Controls
            If TypeOf row Is RoundedPanel AndAlso row.Tag IsNot Nothing AndAlso TypeOf row.Tag Is String Then
                Dim searchable As String = CStr(row.Tag)
                row.Visible = (q = "") OrElse searchable.Contains(q)
            End If
        Next
    End Sub

    Private Sub BuildReagentTable()
        Dim panelWidth As Integer = Me.ClientSize.Width - sidebar.Width - 72
        Dim rowH As Integer = 46
        Dim headerH As Integer = 44
        Dim colHeaderH As Integer = 30
        Dim tableTop As Integer = 148
        Dim panelHeight As Integer = headerH + colHeaderH + reagents.Length * rowH + 12

        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(14, 17, 34)
        panel.BorderColor = Color.FromArgb(32, 37, 60)
        panel.Location = New Point(36, tableTop)
        panel.Size = New Size(panelWidth, panelHeight)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        content.Controls.Add(panel)

        Dim flaskIcon As New Panel()
        flaskIcon.Size = New Size(16, 16)
        flaskIcon.Location = New Point(18, 15)
        AddHandler flaskIcon.Paint, Sub(s, e) DrawNavIcon(e.Graphics, "flask", Color.FromArgb(150, 130, 240))
        panel.Controls.Add(flaskIcon)

        tableHeaderLbl = New Label()
        tableHeaderLbl.Text = "Reagent shelf"
        tableHeaderLbl.Font = New Font("Segoe UI", 10.5, FontStyle.Bold)
        tableHeaderLbl.ForeColor = Color.White
        tableHeaderLbl.AutoSize = True
        tableHeaderLbl.Location = New Point(42, 13)
        panel.Controls.Add(tableHeaderLbl)

        Dim countBadge As New RoundedPanel()
        countBadge.CornerRadius = 9
        countBadge.FillColor = Color.FromArgb(24, 28, 50)
        countBadge.BorderColor = Color.FromArgb(40, 45, 70)
        countBadge.Size = New Size(64, 22)
        countBadge.Location = New Point(panelWidth - 64 - 16, 11)
        countBadge.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        panel.Controls.Add(countBadge)

        Dim lblCount As New Label()
        lblCount.Text = reagents.Length & " items"
        lblCount.Font = New Font("Segoe UI", 8)
        lblCount.ForeColor = Color.FromArgb(160, 168, 190)
        lblCount.AutoSize = True
        lblCount.Location = New Point(9, 4)
        countBadge.Controls.Add(lblCount)

        ' column x-positions (relative to panel)
        Dim xReagent As Integer = 20
        Dim xFormula As Integer = 300
        Dim xConc As Integer = 460
        Dim xHazard As Integer = 610
        Dim xAction As Integer = panelWidth - 108

        Dim colY As Integer = headerH
        AddColumnLabel(panel, "REAGENT", xReagent, colY)
        AddColumnLabel(panel, "FORMULA", xFormula, colY)
        AddColumnLabel(panel, "CONCENTRATION", xConc, colY)
        AddColumnLabel(panel, "HAZARD", xHazard, colY)
        AddColumnLabel(panel, "ACTION", xAction, colY, ContentAlignment.MiddleRight, panelWidth - 20 - xAction)

        Dim divider As New Panel()
        divider.BackColor = Color.FromArgb(28, 32, 54)
        divider.Height = 1
        divider.Location = New Point(0, headerH + colHeaderH - 4)
        divider.Width = panelWidth
        divider.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        panel.Controls.Add(divider)

        Dim rowY As Integer = headerH + colHeaderH
        For Each r In reagents
            BuildReagentRow(panel, r.Item1, r.Item2, r.Item3, r.Item4, r.Item5, rowY, rowH, panelWidth,
                             xReagent, xFormula, xConc, xHazard, xAction)
            rowY += rowH
        Next
    End Sub

    Private Sub AddColumnLabel(panel As Panel, text As String, x As Integer, y As Integer,
                                Optional align As ContentAlignment = ContentAlignment.MiddleLeft,
                                Optional forcedWidth As Integer = 0)
        Dim lbl As New Label()
        lbl.Text = text
        lbl.Font = New Font("Segoe UI", 7.5, FontStyle.Bold)
        lbl.ForeColor = Color.FromArgb(120, 128, 155)
        If forcedWidth > 0 Then
            lbl.AutoSize = False
            lbl.Size = New Size(forcedWidth, 18)
            lbl.TextAlign = align
            lbl.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Else
            lbl.AutoSize = True
        End If
        lbl.Location = New Point(x, y)
        panel.Controls.Add(lbl)
    End Sub

    Private Sub BuildReagentRow(panel As Panel, name As String, formula As String, conc As String,
                                 hazard As String, dotColor As Color, y As Integer, h As Integer, panelWidth As Integer,
                                 xReagent As Integer, xFormula As Integer, xConc As Integer, xHazard As Integer, xAction As Integer)

        Dim row As New RoundedPanel()
        row.CornerRadius = 0
        row.DrawBorder = False
        row.FillColor = panel.BackColor
        row.Location = New Point(4, y)
        row.Size = New Size(panelWidth - 8, h - 2)
        row.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        row.Tag = (name & " " & formula).ToLowerInvariant()
        panel.Controls.Add(row)

        AddHandler row.MouseEnter, Sub()
                                        row.FillColor = Color.FromArgb(19, 23, 44)
                                        row.Invalidate()
                                    End Sub
        AddHandler row.MouseLeave, Sub()
                                        row.FillColor = panel.BackColor
                                        row.Invalidate()
                                    End Sub

        Dim dot As New Panel()
        dot.Size = New Size(12, 12)
        dot.Location = New Point(xReagent, (h - 2 - 12) \ 2)
        AddHandler dot.Paint, Sub(s, e)
                                   e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                   Using br As New SolidBrush(dotColor)
                                       e.Graphics.FillEllipse(br, 0, 0, 11, 11)
                                   End Using
                               End Sub
        row.Controls.Add(dot)

        Dim lblName As New Label()
        lblName.Text = name
        lblName.Font = New Font("Segoe UI", 9.5, FontStyle.Bold)
        lblName.ForeColor = Color.White
        lblName.AutoSize = True
        lblName.Location = New Point(xReagent + 22, (h - 2) \ 2 - 9)
        row.Controls.Add(lblName)

        Dim lblFormula As New Label()
        lblFormula.Text = formula
        lblFormula.Font = New Font("Segoe UI", 9.5)
        lblFormula.ForeColor = Color.FromArgb(150, 130, 240)
        lblFormula.AutoSize = True
        lblFormula.Location = New Point(xFormula, (h - 2) \ 2 - 8)
        row.Controls.Add(lblFormula)

        Dim lblConc As New Label()
        lblConc.Text = conc
        lblConc.Font = New Font("Segoe UI", 9.5)
        lblConc.ForeColor = Color.FromArgb(190, 196, 216)
        lblConc.AutoSize = True
        lblConc.Location = New Point(xConc, (h - 2) \ 2 - 8)
        row.Controls.Add(lblConc)

        Dim badge As New RoundedPanel()
        badge.CornerRadius = 8
        badge.DrawBorder = False
        Dim hazardColor As Color = Me.HazardColor(hazard)
        badge.FillColor = BlendWithBackground(hazardColor, panel.BackColor, 0.2F)
        badge.Size = New Size(TextRenderer.MeasureText(hazard, New Font("Segoe UI", 8, FontStyle.Bold)).Width + 20, 22)
        badge.Location = New Point(xHazard, (h - 2 - 22) \ 2)
        row.Controls.Add(badge)

        Dim lblHazard As New Label()
        lblHazard.Text = hazard
        lblHazard.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        lblHazard.ForeColor = hazardColor
        lblHazard.AutoSize = True
        lblHazard.Location = New Point(10, 4)
        badge.Controls.Add(lblHazard)

        Dim lblAction As New Label()
        lblAction.Text = "Pour into…"
        lblAction.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        lblAction.ForeColor = Color.FromArgb(150, 130, 240)
        lblAction.AutoSize = False
        lblAction.Size = New Size(panelWidth - 20 - xAction, 20)
        lblAction.TextAlign = ContentAlignment.MiddleRight
        lblAction.Location = New Point(xAction, (h - 2) \ 2 - 9)
        lblAction.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblAction.Cursor = Cursors.Hand
        AddHandler lblAction.Click, Sub() MessageBox.Show($"Pouring {name} ({formula}) opens a volume dialog, then animates the transfer into the selected 3D vessel.", "ChemLab Virtual")
        row.Controls.Add(lblAction)
    End Sub

    ''' <summary>Blends a color toward a background at a given opacity, returning an opaque
    ''' result — child controls don't alpha-composite against their parent, so this avoids
    ''' badges that render against the wrong backdrop.</summary>
    Private Function BlendWithBackground(fg As Color, bg As Color, alpha As Single) As Color
        Dim r As Integer = CInt(fg.R * alpha + bg.R * (1 - alpha))
        Dim g As Integer = CInt(fg.G * alpha + bg.G * (1 - alpha))
        Dim b As Integer = CInt(fg.B * alpha + bg.B * (1 - alpha))
        Return Color.FromArgb(r, g, b)
    End Function

    Private Function HazardColor(hazard As String) As Color
        Select Case hazard
            Case "Low risk"
                Return Color.FromArgb(120, 220, 170)
            Case Else
                Return Color.FromArgb(232, 168, 92)
        End Select
    End Function

    Private Sub BuildInfoBanner()
        Dim panelWidth As Integer = Me.ClientSize.Width - sidebar.Width - 72
        Dim rowH As Integer = 46
        Dim headerH As Integer = 44
        Dim colHeaderH As Integer = 30
        Dim tableTop As Integer = 148
        Dim tableHeight As Integer = headerH + colHeaderH + reagents.Length * rowH + 12
        Dim bannerY As Integer = tableTop + tableHeight + 20

        Dim banner As New RoundedPanel()
        banner.CornerRadius = 12
        banner.FillColor = Color.FromArgb(16, 20, 40)
        banner.BorderColor = Color.FromArgb(36, 41, 66)
        banner.Location = New Point(36, bannerY)
        banner.Size = New Size(panelWidth, 56)
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
        lblInfo.Text = "Pouring opens a volume dialog (ml) and animates the liquid transfer into the selected 3D vessel."
        lblInfo.Font = New Font("Segoe UI", 9)
        lblInfo.ForeColor = Color.FromArgb(160, 168, 190)
        lblInfo.Location = New Point(52, 18)
        lblInfo.Size = New Size(panelWidth - 70, 34)
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