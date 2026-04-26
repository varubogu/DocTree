using DocTree.App;
using DocTree.Forms;

namespace DocTree
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.ThreadException += (_, e) => ShowFatal("UI スレッドで未処理例外が発生しました。", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                ShowFatal("未処理例外が発生しました。", e.ExceptionObject as Exception);

            App.AppContext appContext;
            try
            {
                appContext = App.AppContext.Bootstrap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "設定ファイルの読み込みに失敗しました。\n\n" + ex.Message +
                    "\n\n空の設定で起動します。問題を修正後、メニューから [設定を再読み込み] してください。",
                    "DocTree", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                appContext = App.AppContext.BootstrapEmpty();
            }

            Application.Run(new MainForm(appContext));
        }

        private static void ShowFatal(string title, Exception? ex)
        {
            try
            {
                MessageBox.Show((ex?.ToString() ?? "(詳細不明)"), title,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { /* メッセージ自体が出せない場合は諦める */ }
        }
    }
}
