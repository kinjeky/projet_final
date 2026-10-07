using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    void Start()
    {
        // Le jeu bloque la souris au centre : on la libère pour pouvoir cliquer sur les boutons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // À appeler depuis le On Click () d'un bouton, avec le nom exact de la scène
    public void ChargerScene(string nomScene)
    {
        SceneManager.LoadScene(nomScene);
    }

    public void Quitter()
    {
        Application.Quit();
    }
}
