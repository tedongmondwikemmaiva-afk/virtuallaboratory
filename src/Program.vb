module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Start the full application flow (splash -> login -> home/admin).
        Try
            Application.Run(New MainAppContext())
        Catch ex As Exception
            MessageBox.Show($"Error launching application:\n{ex.ToString()}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Module

' ApplicationContext that shows the splash once, then presents the login -> home/admin flow.
Public Class MainAppContext
    Inherits ApplicationContext

    Public Sub New()
        ' Show splash modally first
        Using splash As New SplashForm()
            splash.ShowDialog()
        End Using

        ' Enter the login loop. When the user closes without signing in, exit the app.
        Dim keepGoing As Boolean = True
        Do While keepGoing
            keepGoing = False

            Using login As New LoginForm()
                Dim loginResult = login.ShowDialog()

                If loginResult <> DialogResult.OK OrElse login.Outcome = LoginOutcome.Cancelled Then
                    Exit Do
                End If

                If login.SignedInRole = "Admin" Then
                    Using adminForm As New AdminDashboardForm(login.SignedInName)
                        Dim r = adminForm.ShowDialog()
                        keepGoing = (r = DialogResult.Retry)
                    End Using
                Else
                    Using homeForm As New HomeForm(login.SignedInName, login.SignedInRole)
                        Dim r = homeForm.ShowDialog()
                        keepGoing = (r = DialogResult.Retry)
                    End Using
                End If
            End Using
        Loop

        ' All done -> exit application
        Me.ExitThread()
    End Sub
End Class
