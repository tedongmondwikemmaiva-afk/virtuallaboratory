Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        ' Show the splash first as a dialog. When it closes, show the login form as the main application form.
        Dim splash As New SplashForm()
        splash.ShowDialog()

        Application.Run(New Form1())
    End Sub
End Module