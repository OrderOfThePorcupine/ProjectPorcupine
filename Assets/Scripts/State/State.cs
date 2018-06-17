﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;

namespace ProjectPorcupine.Entities.States
{
    public abstract class State
    {
        protected Character character;
        private bool exstensiveLog = false;

        public State(string name, Character character, State nextState)
        {
            Name = name;
            this.character = character;
            NextState = nextState;
        }

        public string Name { get; protected set; }

        public State NextState { get; protected set; }

        public virtual void Enter()
        {
            DebugLog(" - Enter");
        }

        public abstract void Update(float deltaTime);

        public virtual void Exit()
        {
            DebugLog(" - Exit");
        }

        public virtual void Interrupt()
        {
            DebugLog(" - Interrupt");

            if (NextState != null)
            {
                NextState.Interrupt();
                NextState = null;
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}State]", Name);
        }

        protected void Finished()
        {
            DebugLog(" - Finished");
            character.SetState(NextState);
        }

        #region Debug

        protected void DebugLog(string message, params object[] par)
        {
            if (!exstensiveLog)
            {
                return;
            }

            string prefixedMessage = string.Format("{0}, {1} {2}: {3}", character.GetName(), character.ID, StateStack(), message);
            UnityDebugger.Debugger.LogFormat("Character", prefixedMessage, par);
        }

        private string StateStack()
        {
            List<string> names = new List<string> { Name };
            State state = this;
            while (state.NextState != null)
            {
                state = state.NextState;
                names.Insert(0, state.Name);
            }

            return string.Join(".", names.ToArray());
        }

        #endregion
    }
}