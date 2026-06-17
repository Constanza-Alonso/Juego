using UnityEngine;

public class ShapeSelectButton : MonoBehaviour
{
    [SerializeField] private MainMenuController menuController;
    [SerializeField] private int shapeIndex;

    public void SelectShape()
    {
        if (menuController != null)
        {
            menuController.SelectLuxShape(shapeIndex);
        }
    }
}
