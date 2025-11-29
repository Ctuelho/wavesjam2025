using UnityEngine;
using UnityEngine.UI;

public class ObserverController : MonoBehaviour
{
    private Slot targetSlot;

    public void SetTargetSlot(Slot slot)
    {
        targetSlot = slot;
    }

    // Este método seria chamado pelo Evento OnClick de um botão na UI do Controller
    public void OnCycleButtonClicked()
    {
        if (targetSlot != null)
        {
            targetSlot.CycleDecayType();
            targetSlot.CycleRange();
        }
    }
}