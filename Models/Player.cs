using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LuaEmuPlayer.Models.Emulator;

namespace LuaEmuPlayer.Models
{
    internal class Player
    {
        public enum State
        {
            INITIAL,
            ONLY_EMULATOR,
            GAME_ACTIVE,
        }

        public delegate void EmuStateChangeDelegate(State state);
        public EmuStateChangeDelegate EmuStateChange;

        Emulator _emulator = new();

        public Player(EmuStateChangeDelegate emuStateChange)
        {
            Task.Run(Scan);
            EmuStateChange = emuStateChange;
        }

        async Task Scan()
        {
            EmuStateChange(State.INITIAL);

            int delayMs = 1000;
            while (true)
            {
                try
                {
                    delayMs = 1000;
                    switch (_emulator.Prepare())
                    {
                        case PrepareResult.NOT_FOUND:
                            break;
                        case PrepareResult.ONLY_EMULATOR:
                            EmuStateChange(State.ONLY_EMULATOR);
                            break;
                        case PrepareResult.OK:
                            EmuStateChange(State.GAME_ACTIVE);
                            break;
                    }
                }
                finally
                {
                    await Task.Delay(delayMs);
                }
            }
        }
    }
}
