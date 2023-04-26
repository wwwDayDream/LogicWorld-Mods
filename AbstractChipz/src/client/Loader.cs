using Chipz.Client.ComponentCode;
using GameDataAccess;
using LICC;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.References;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Chipz.Client
{
    public class ChipzClientMod : ClientMod
    {
        // Thanks to Ecconia for this method
        // https://github.com/Ecconia/Ecconia-LogicWorld-Mods/blob/master/EcconiasChaosClientMod/EcconiasChaosClientMod/src/Client/ThisIsBlack.cs#L131
        public static ComponentSelection ExtractMultiSelectedObjects()
        {
            FieldInfo field = typeof(MultiSelector).GetField("CurrentSelection", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                LConsole.WriteLine("Could not get selection of components, as the 'CurrentSelection' field could not be found. Report this issue to the mod maintainer.");
                return null;
            }
            object value = field.GetValue(null);
            if (value == null)
            {
                LConsole.WriteLine("Could not get selection of components, as the current selection is 'null'. Report this issue to the mod maintainer.");
                return null;
            }
            if (!(value is ComponentSelection selection))
            {
                LConsole.WriteLine("Could not get selection of components, as the current selection is a weird type '" + value.GetType() + "'. Report this issue to the mod maintainer.");
                return null;
            }
            if (selection.Count == 0)
            {
                LConsole.WriteLine("Could not get selection of components, as nothing is selected? Report this issue to the mod maintainer.");
                return null;
            }
            return selection;
        }
        public static IEnumerable<T> RecurseSearchForChips<T>(IEnumerable<ComponentAddress> search) where T : class
        {
            IClientWorld world = Instances.MainWorld;
            if (search.Count() > 0)
            {
                IEnumerable<T> chipsToHit = search
                    .Select(cAddy => world.Renderer.Entities.GetClientCode(cAddy))
                    .Where(cCode => cCode is T)
                    .Select(cCode => cCode as T);

                foreach (T chip in chipsToHit)
                {
                    yield return chip;
                }

                IEnumerable<T> subChipsToHit = RecurseSearchForChips<T>(search
                    .Select(cAddy => world.Renderer.Entities.GetClientCode(cAddy))
                    .Where(cCode => cCode.Component.ChildCount > 0)
                    .SelectMany(cCode => cCode.Component.EnumerateChildren()));
                foreach (T sChip in subChipsToHit)
                {
                    yield return sChip;
                }
            }
        }
        protected override void Initialize()
        {
            Logger.Info("Chipz mod initialized");
        }
    }
}