using System.Text.RegularExpressions;
using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Utilities;
using SDL2;

namespace DeadCellsMultiplayerX.Client.UI.Modes
{
    internal class DefaultMode : ModeConfig, IOnGameExit
    {
        private string lastClipboard = "";
        private bool isJoining = false;

        public DefaultMode(LobbyMenu manager) : base(manager, "默认联机") { }

        public override void BuildContent(FlowBox right, int panelW)
            => Manager.LoadImageTorightFlow("DeadCellsMultiplayerX/Image/lobbyTile.png");

        public override void OnHost(Action onend) => StartConnect(true, onend);
        public override void OnClient(Action canEnter) => StartConnect(false, null, canEnter);

        public override void OnHostLeave() => Manager.client.CurrentHostClient?.Dispose();
        public override void OnClientLeave() => Manager.client.CurrentGuestClient?.Quit();

        public override void Update()
        {
            if (Manager.GetMe() != null) return;

            if (!isJoining && TryParseInvite(out var ip, out var port))
            {
                var newip = GenerateInvite(ip, port);
                if (lastClipboard != newip)
                {
                    lastClipboard = newip;
                    ShowJoinConfirm(ip, port);
                }
            }
        }

        void IOnGameExit.OnGameExit()
        {
            var me = Manager.GetMe();
            if (me == null) return;
            if (me.IsHost) OnHostLeave(); else OnClientLeave();
        }

        private void StartConnect(bool asHost, Action? onEnd = null, Action? canEnter = null)
        {
            string defaultIp = "";
#if DEBUG
            defaultIp = "127.0.0.1:12345";
#endif
            new TextInput(
                Manager,
                "输入ip及端口".AsHaxeString(),
                "test".AsHaxeString(),
                defaultIp.AsHaxeString(),
                (str) =>
                {
                    if (!TryParseIpPort(str.ToString(), out var ip, out var port))
                    {
                        ShowError(asHost ? () => OnHost(onEnd!) : () => OnClient(canEnter!));
                        return;
                    }

                    Manager.LoaddingIn(asHost ? "创建房间..." : "加入房间...", async () =>
                    {
                        if (asHost)
                        {
                            await ClientMain.Instance.StartHost(ip, port);
                            CopyInvite(ip, port);
                        }
                        else
                        {
                            await ClientMain.Instance.StartGuest(ip, port);
                            ClientMain.Instance.CurrentGuestClient?.SetReady(true);
                        }
                        onEnd?.Invoke();
                        canEnter?.Invoke();
                    });
                    Manager.delayer.addMs(null, () => Manager.LoaddingOut(), 3000);
                }, null, null, null);
        }


        private bool TryParseInvite(out string ip, out int port)
        {
            ip = null!; port = 0;
            if (SDL.SDL_HasClipboardText() != SDL.SDL_bool.SDL_TRUE) return false;
            var match = Regex.Match(SDL.SDL_GetClipboardText(), @"IP:\s*([\d.]+)\s*端口:\s*(\d+)");
            if (!match.Success) return false;
            ip = match.Groups[1].Value;
            return int.TryParse(match.Groups[2].Value, out port);
        }

        private bool TryParseIpPort(string input, out string ip, out int port)
        {
            ip = null!; port = 0;
            var parts = input.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;
            if (!int.TryParse(parts[^1], out port) || port < 0 || port > 65535) return false;
            ip = parts[0];
            return true;
        }

        private string GenerateInvite(string ip, int port)
            => $"复制此文字打开死亡细胞联机模组即可加入我的房间 IP: {ip} 端口: {port}";

        private void CopyInvite(string ip, int port)
        {
            lastClipboard = GenerateInvite(ip, port);
            SDL.SDL_SetClipboardText(lastClipboard);
            new Confirmation(Manager, "邀请已复制".AsHaxeString(), ()=> { }, null, "好的".AsHaxeString(), null, null);
        }


        private void ShowJoinConfirm(string ip, int port)
        {
            isJoining = true;
            new Confirmation(
                Manager,
                $"检测到邀请：{ip}:{port}\n是否加入？".AsHaxeString(),
                () =>
                {
                    isJoining = false;
                    Manager.LoaddingIn("加入房间...", async () =>
                    {
                        await ClientMain.Instance.StartGuest(ip, port);
                        ClientMain.Instance.CurrentGuestClient?.SetReady(true);
                        Manager.RefreshFlow(false);
                        Manager.AddClientButtons();
                    });
                    Manager.delayer.addMs(null, () => Manager.LoaddingOut(), 3000);
                },
                () => isJoining = false,
                "加入".AsHaxeString(),
                "取消".AsHaxeString(),
                null
            );
        }

        private void ShowError(HlAction retry)
        {
            var pop = new ModalPopUp(Ref<bool>.In(false), null);
            pop.text("请输入正确IP及端口".AsHaxeString(), null, default);
            pop.onClose = retry;
        }
    }
}