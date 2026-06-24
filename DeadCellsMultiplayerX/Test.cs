using DeadCellsMultiplayerX.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX
{
    internal class Test
    {
        private static bool started = false;
        public static async void Start()
        {
            // if(started)
            // {
            //     return;
            // }
            // started = true;

            await ClientMain.Instance.StartHost("127.0.0.1", 12345);

            var guest = ClientMain.Instance.CurrentGuestClient!;
            guest.SetReady(true);

            await Task.Delay(500);

            //await ClientMain.Instance.CurrentHostClient!.StartGame();
        }
    }
}
