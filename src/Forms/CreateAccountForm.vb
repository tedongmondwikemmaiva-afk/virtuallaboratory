Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Class CreateAccountForm
    Inherits Form

    Private txtName As TextBox
    Private txtEmail As TextBox
    Private txtPass As TextBox
    Private txtConfirm As TextBox
    Private cmbRole As ComboBox
    Private lblStatus As Label
    Private card As RoundedPanel

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(460, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(6, 8, 18)
        Me.KeyPreview = True
        AddHandler Me.KeyDown, Sub(s, e) If e.KeyCode = Keys.Escape Then Me.Close()

        card = New RoundedPanel()
        card.CornerRadius = 18
        card.FillColor = Color.FromArgb(18, 22, 40)
        card.BorderColor = Color.FromArgb(42, 47, 70)
        card.Dock = DockStyle.Fill
        Me.Controls.Add(card)

        BuildUI()
    End Sub

    Private Sub BuildUI()
        Dim lblTitle As New Label() With {.Text = "Create your account", .Font = New Font("Segoe UI", 17, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(32, 30)}
        card.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {.Text = "Join ChemLab Virtual with your institution details.", .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(160, 168, 190), .AutoSize = True, .Location = New Point(32, 66)}
        card.Controls.Add(lblSub)

        Dim y As Integer = 106
        txtName = CreateInputField(32, y, "Full name", False) : y += 62
        txtEmail = CreateInputField(32, y, "Institution email", False) : y += 62
        txtPass = CreateInputField(32, y, "Password", True) : y += 62
        txtConfirm = CreateInputField(32, y, "Confirm password", True) : y += 62

        Dim lblRole As New Label() With {.Text = "I am a...", .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(160, 168, 190), .AutoSize = True, .Location = New Point(32, y)}
        card.Controls.Add(lblRole)
        y += 24

        cmbRole = New ComboBox()
        cmbRole.DropDownStyle = ComboBoxStyle.DropDownList
        cmbRole.Font = New Font("Segoe UI", 10.5)
        cmbRole.Items.AddRange({"Student", "Teacher"})
        cmbRole.SelectedIndex = 0
        cmbRole.Size = New Size(396, 30)
        cmbRole.Location = New Point(32, y)
        cmbRole.FlatStyle = FlatStyle.Flat
        cmbRole.BackColor = Color.FromArgb(24, 28, 48)
        cmbRole.ForeColor = Color.White
        card.Controls.Add(cmbRole)
        y += 48

        Dim btnCreate As New GradientButton() With {.Text = "Create account", .Size = New Size(396, 46), .Location = New Point(32, y)}
        AddHandler btnCreate.Click, AddressOf BtnCreate_Click
        card.Controls.Add(btnCreate)
        y += 58

        lblStatus = New Label() With {.Text = "", .Font = New Font("Segoe UI", 9), .Location = New Point(32, y), .Size = New Size(396, 40)}
        card.Controls.Add(lblStatus)
        y += 44

        Dim lnkBack As New LinkLabel() With {.Text = "Already have an account? Sign in", .Font = New Font("Segoe UI", 9.5), .LinkColor = Color.FromArgb(56, 214, 255), .ActiveLinkColor = Color.FromArgb(90, 225, 255), .AutoSize = True}
        lnkBack.Location = New Point(32, y)
        AddHandler lnkBack.LinkClicked, Sub()
                                             Me.DialogResult = DialogResult.Cancel
                                             Me.Close()
                                         End Sub
        card.Controls.Add(lnkBack)
    End Sub

    Private Sub BtnCreate_Click(sender As Object, e As EventArgs)
        Dim nameVal = ValueOrEmpty(txtName, "Full name")
        Dim emailVal = ValueOrEmpty(txtEmail, "Institution email")
        Dim passVal = ValueOrEmpty(txtPass, "Password")
        Dim confirmVal = ValueOrEmpty(txtConfirm, "Confirm password")

        If nameVal.Length = 0 OrElse emailVal.Length = 0 OrElse passVal.Length = 0 Then
            ShowStatus("Please fill in all fields.", False)
            Return
        End If
        If Not emailVal.Contains("@") Then
            ShowStatus("Please enter a valid email address.", False)
            Return
        End If
        If passVal.Length < 6 Then
            ShowStatus("Password should be at least 6 characters.", False)
            Return
        End If
        If passVal <> confirmVal Then
            ShowStatus("Passwords do not match.", False)
            Return
        End If

        ' TODO: replace with a real call to your account-creation/backend service.
        ShowStatus($"Account created for {nameVal} as {cmbRole.SelectedItem} (demo).", True)
        Me.DialogResult = DialogResult.OK
    End Sub

    Private Sub ShowStatus(msg As String, success As Boolean)
        lblStatus.ForeColor = If(success, Color.FromArgb(120, 220, 170), Color.FromArgb(220, 140, 140))
        lblStatus.Text = msg
    End Sub

    Private Function ValueOrEmpty(tb As TextBox, placeholder As String) As String
        Return If(tb.Text = placeholder, "", tb.Text)
    End Function

    Private Function CreateInputField(x As Integer, y As Integer, placeholder As String, isPassword As Boolean) As TextBox
        Dim fieldPanel As New RoundedPanel()
        fieldPanel.CornerRadius = 10
        fieldPanel.FillColor = Color.FromArgb(24, 28, 48)
        fieldPanel.BorderColor = Color.FromArgb(46, 51, 76)
        fieldPanel.Size = New Size(396, 48)
        fieldPanel.Location = New Point(x, y)
        card.Controls.Add(fieldPanel)

        Dim tb As New TextBox()
        tb.BorderStyle = BorderStyle.None
        tb.BackColor = fieldPanel.FillColor
        tb.Font = New Font("Segoe UI", 11)
        tb.Location = New Point(16, 14)
        tb.Width = 364
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

End Class
