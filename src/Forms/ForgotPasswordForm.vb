Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Class ForgotPasswordForm
    Inherits Form

    Private txtEmail As TextBox
    Private lblStatus As Label
    Private card As RoundedPanel

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(460, 420)
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
        Dim iconBox As New Panel() With {.Size = New Size(46, 46), .Location = New Point(32, 30)}
        AddHandler iconBox.Paint, Sub(s, e)
                                      Dim g = e.Graphics
                                      g.SmoothingMode = SmoothingMode.AntiAlias
                                      Dim rect As New Rectangle(0, 0, 45, 45)
                                      Using path = RoundedRectPath(rect, 14)
                                          Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
                                              g.FillPath(br, path)
                                          End Using
                                      End Using
                                      Using pen As New Pen(Color.White, 1.8F)
                                          g.DrawArc(pen, 15, 12, 15, 15, 180, 180)
                                          g.DrawRectangle(pen, 13, 21, 19, 14)
                                      End Using
                                  End Sub
        card.Controls.Add(iconBox)

        Dim lblTitle As New Label() With {.Text = "Reset your password", .Font = New Font("Segoe UI", 16, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(32, 90)}
        card.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {
            .Text = "Enter your institution email and we'll send you a link to reset your password.",
            .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(160, 168, 190),
            .Location = New Point(32, 124), .Size = New Size(396, 40)
        }
        card.Controls.Add(lblSub)

        txtEmail = CreateInputField(card, 32, 176, 396, "person", "Student ID or email", False)

        Dim btnSend As New GradientButton() With {.Text = "Send reset link", .Size = New Size(396, 46), .Location = New Point(32, 236)}
        AddHandler btnSend.Click, AddressOf BtnSend_Click
        card.Controls.Add(btnSend)

        lblStatus = New Label() With {
            .Text = "", .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(120, 220, 170),
            .Location = New Point(32, 292), .Size = New Size(396, 40), .TextAlign = ContentAlignment.TopLeft
        }
        card.Controls.Add(lblStatus)

        Dim lnkBack As New LinkLabel() With {
            .Text = "← Back to sign in", .Font = New Font("Segoe UI", 9.5), .LinkColor = Color.FromArgb(56, 214, 255),
            .ActiveLinkColor = Color.FromArgb(90, 225, 255), .AutoSize = True
        }
        lnkBack.Location = New Point(32, 350)
        AddHandler lnkBack.LinkClicked, Sub() Me.Close()
        card.Controls.Add(lnkBack)
    End Sub

    Private Sub BtnSend_Click(sender As Object, e As EventArgs)
        Dim emailVal As String = If(txtEmail.Text = "Student ID or email", "", txtEmail.Text)
        If emailVal.Trim().Length = 0 Then
            lblStatus.ForeColor = Color.FromArgb(220, 140, 140)
            lblStatus.Text = "Please enter your Student ID or email first."
            Return
        End If
        ' TODO: call your real password-reset/email service here.
        lblStatus.ForeColor = Color.FromArgb(120, 220, 170)
        lblStatus.Text = $"If an account exists for '{emailVal}', a reset link has been sent."
    End Sub

    ' Local copy of the shared input-field builder (kept self-contained per dialog).
    Private Function CreateInputField(parent As Control, x As Integer, y As Integer, width As Integer,
                                       iconType As String, placeholder As String, isPassword As Boolean) As TextBox
        Dim fieldPanel As New RoundedPanel()
        fieldPanel.CornerRadius = 10
        fieldPanel.FillColor = Color.FromArgb(24, 28, 48)
        fieldPanel.BorderColor = Color.FromArgb(46, 51, 76)
        fieldPanel.Size = New Size(width, 48)
        fieldPanel.Location = New Point(x, y)
        parent.Controls.Add(fieldPanel)

        Dim iconPanel As New Panel() With {.Size = New Size(20, 20), .Location = New Point(14, 14), .BackColor = Color.Transparent}
        AddHandler iconPanel.Paint, Sub(s, e)
                                        Dim g = e.Graphics
                                        g.SmoothingMode = SmoothingMode.AntiAlias
                                        Using pen As New Pen(Color.FromArgb(150, 158, 180), 1.6F)
                                            If iconType = "person" Then
                                                g.DrawEllipse(pen, 5, 1, 10, 10)
                                                g.DrawArc(pen, 1, 11, 18, 14, 180, 180)
                                            Else
                                                g.DrawArc(pen, 4, 0, 12, 12, 180, 180)
                                                g.DrawRectangle(pen, 2, 9, 16, 11)
                                            End If
                                        End Using
                                    End Sub
        fieldPanel.Controls.Add(iconPanel)

        Dim tb As New TextBox()
        tb.BorderStyle = BorderStyle.None
        tb.BackColor = fieldPanel.FillColor
        tb.Font = New Font("Segoe UI", 11)
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

End Class
