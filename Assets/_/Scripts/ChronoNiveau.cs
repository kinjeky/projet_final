using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Chronomètre de speedrun : un par niveau, garde le meilleur temps de chaque scène.
// Appeler TerminerNiveau() (ex. : Invoke Events du Collider Event System) quand le niveau est réussi.
public class ChronoNiveau : MonoBehaviour
{
    public TMP_Text texteChrono;    // Temps en cours
    public TMP_Text texteMeilleur;  // Meilleur temps du niveau

    float temps;
    bool enCours = true;

    // Chaque scène a son propre record (ex. : "MeilleurTemps_niveau_02")
    string Cle => "MeilleurTemps_" + SceneManager.GetActiveScene().name;

    void Start()
    {
        AfficherMeilleur();
    }

    void Update()
    {
        if (!enCours) return;

        temps += Time.deltaTime;
        if (texteChrono != null) texteChrono.text = Formater(temps);
    }

    // À appeler quand le joueur réussit le niveau
    public void TerminerNiveau()
    {
        if (!enCours) return; // Évite d'enregistrer deux fois
        enCours = false;

        float meilleur = PlayerPrefs.GetFloat(Cle, float.MaxValue);
        if (temps < meilleur)
        {
            PlayerPrefs.SetFloat(Cle, temps);
            PlayerPrefs.Save();
            if (texteMeilleur != null) texteMeilleur.text = "Nouveau record : " + Formater(temps) + " !";
        }
        else
        {
            AfficherMeilleur();
        }
    }

    // Optionnel : efface le record de ce niveau (pratique pour tester)
    public void EffacerRecord()
    {
        PlayerPrefs.DeleteKey(Cle);
        AfficherMeilleur();
    }

    void AfficherMeilleur()
    {
        if (texteMeilleur == null) return;

        if (PlayerPrefs.HasKey(Cle))
            texteMeilleur.text = "Record : " + Formater(PlayerPrefs.GetFloat(Cle));
        else
            texteMeilleur.text = "Record : --:--.--";
    }

    // Format mm:ss.cc (ex. : 01:23.45)
    static string Formater(float secondes)
    {
        int minutes = (int)(secondes / 60f);
        float reste = secondes % 60f;
        return string.Format("{0:00}:{1:00.00}", minutes, reste);
    }
}
