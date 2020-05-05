﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AppleWirelessKeyboardCore.Services;

namespace AppleWirelessKeyboardCore.Keyboard
{
    public class KeyboardHandler
    {
        public KeyboardHandler()
        {
            this.Inject();
        }

        [ImportMany]
        public IEnumerable<IInputAdapter> Adapters { get; set; } = null!;

        [ImportMany]
        public IEnumerable<Lazy<Action<KeyboardEvent>, IFunctionalityModuleExportMetadata>> Modules { get; set; } = null!;

        [ImportMany]
        public IEnumerable<IInputFilter> Filters { get; set; } = null!;

        public bool Fn { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Win { get; set; }
        public bool Shift { get; set; }
        public bool Eject { get; set; }
        public bool FMode { get; set; }
        public bool Power { get; set; }

        public void Start()
        {
            foreach (IInputAdapter adapter in Adapters)
            {
                adapter.Start();
                adapter.Fn += (pressed) => { if (Filters.All(f => f.Fn(pressed))) Fn = pressed; };
                adapter.Alt += (pressed) => { if (Filters.All(f => f.Alt(pressed))) Alt = pressed; };
                adapter.Ctrl += (pressed) => { if (Filters.All(f => f.Ctrl(pressed))) Ctrl = pressed; };
                adapter.Win += (pressed) => { if (Filters.All(f => f.Win(pressed))) Win = pressed; };
                adapter.Shift += (pressed) => { if (Filters.All(f => f.Shift(pressed))) Shift = pressed; };
                adapter.Eject += (pressed) => { if (Filters.All(f => f.Eject(pressed))) Eject = pressed; };
                adapter.Power += (pressed) => { if (Filters.All(f => f.Power(pressed))) Power = pressed; };
                adapter.Key += ProcessKey;
            }
        }

        public void Stop()
        {
            foreach (IInputAdapter adapter in Adapters)
            {
                adapter.Stop();
            }
        }

        bool ProcessKey(Key key, bool pressed)
        {
            if (!Filters.All(f => f.Key(key, pressed)))
                return false;

            bool handled = false;
            foreach (KeyBinding binding in SettingsService.Default.KeyBindings
                .Where(b => b.Key == key 
                    && (!b.Alt || Alt) 
                    && (!b.Ctrl || Ctrl) 
                    && (!b.Win || Win) 
                    && (!b.Fn || Fn) 
                    && (!b.Shift || Shift) 
                    && (!b.FMode || FMode)))
            {
                Action<KeyboardEvent>? module = Modules.SingleOrDefault(l => l.Metadata.Name == binding.Module)?.Value;
                if (module != null)
                { 
                    Task.Factory.StartNew(() => module(pressed ? KeyboardEvent.Down : KeyboardEvent.Up));
                    handled = true;
                }
            }
            return handled;
        }
    }
}
