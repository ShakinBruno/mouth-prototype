using System.Collections.Generic;

namespace Mouth.Interaction.Electronics
{
    public interface IElectronics
    {
        void ChangeStateOfElectronics(bool isFuseboxActive, bool isEmergencyShutdown, bool updateAnimations);
        IEnumerable<object> GetElectronics();
    }
}