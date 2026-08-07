Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Splash runs once, modally, then closes itself (on 100% or "Skip intro").
        Using splash As New SplashForm()
            splash.ShowDialog()
        End Using

        ' Login <-> Home/Admin loop: logging out from Home/Admin returns here.
        Dim keepGoing As Boolean = True
        Do While keepGoing
            keepGoing = False

            Dim login As New LoginForm()
            Dim loginResult = login.ShowDialog()

            If loginResult <> DialogResult.OK OrElse login.Outcome = LoginOutcome.Cancelled Then
                Exit Do ' user closed the login screen without signing in -> exit app
            End If

            If login.SignedInRole = "Admin" Then
                Using adminForm As New AdminDashboardForm(login.SignedInName)
                    Dim r = adminForm.ShowDialog()
                    keepGoing = (r = DialogResult.Retry) ' Retry == user logged out, go back to login
                End Using
            Else
                Using homeForm As New HomeForm(login.SignedInName, login.SignedInRole)
                    Dim r = homeForm.ShowDialog()
                    keepGoing = (r = DialogResult.Retry)
                End Using
            End If
        Loop
    End Sub
End Module
