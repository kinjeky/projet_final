using UnityEngine;
using UnityEngine.AI;

// Ennemi qui erre (idle / marche), sort son arme quand le joueur approche,
// tire sur lui s'il reste dans la zone, et range son arme s'il s'éloigne.
// À placer sur l'ennemi (avec un NavMeshAgent). Remplace NPC Wander / NPC Follower sur ce personnage.
[RequireComponent(typeof(NavMeshAgent))]
public class EnnemiTireur : MonoBehaviour
{
    [Header("Références")]
    public Animator animator;
    public Transform joueur;              // Vide = cherche l'objet avec le tag Player
    public GameObject arme;               // Le fusil dans la main (désactivé au départ)
    public Transform pointDeTir;          // Bout du canon
    public GameObject balle;              // Prefab de balle (ex. : variante du Bullet du joueur)

    [Header("Errance")]
    public float rayonErrance = 8f;
    public float pauseMin = 2f;
    public float pauseMax = 5f;

    [Header("Combat")]
    public float rayonAggro = 12f;        // Distance à laquelle il sort son arme
    public float marge = 2f;              // Il range son arme seulement au-delà de rayonAggro + marge
    public float dureeSortieArme = 1.2f;  // Longueur de l'animation "sortir l'arme"
    public float dureeRangerArme = 1.2f;  // Longueur de l'animation "ranger l'arme"
    public float cadence = 0.6f;          // Secondes entre deux tirs
    public float vitesseBalle = 40f;
    public float dispersion = 2f;         // Imprécision en degrés (0 = tir parfait)
    public float hauteurVisee = 1.2f;     // Vise le torse du joueur, pas ses pieds
    public bool besoinLigneDeVue = true;  // Ne tire pas à travers les murs

    enum Etat { Errance, SortieArme, Tir, RangementArme }
    Etat etat = Etat.Errance;

    NavMeshAgent agent;
    Vector3 origine;
    float minuterie;
    Collider[] mesColliders;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (joueur == null)
        {
            GameObject j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) joueur = j.transform;
        }
        if (arme != null) arme.SetActive(false);

        origine = transform.position;
        mesColliders = GetComponentsInChildren<Collider>();
        minuterie = Random.Range(pauseMin, pauseMax);
    }

    void Update()
    {
        float distance = joueur != null ? Vector3.Distance(transform.position, joueur.position) : Mathf.Infinity;
        bool dansZone = distance <= rayonAggro;
        bool horsZone = distance > rayonAggro + marge;

        switch (etat)
        {
            case Etat.Errance:
                Errer();
                if (dansZone) SortirArme();
                break;

            case Etat.SortieArme:
                RegarderJoueur();
                minuterie -= Time.deltaTime;
                if (horsZone) RangerArme();
                else if (minuterie <= 0f) { etat = Etat.Tir; minuterie = 0f; }
                break;

            case Etat.Tir:
                RegarderJoueur();
                minuterie -= Time.deltaTime;
                if (minuterie <= 0f)
                {
                    if (!besoinLigneDeVue || VoitJoueur()) Tirer();
                    minuterie = cadence;
                }
                if (horsZone) RangerArme();
                break;

            case Etat.RangementArme:
                minuterie -= Time.deltaTime;
                if (dansZone) SortirArme();
                else if (minuterie <= 0f)
                {
                    if (arme != null) arme.SetActive(false);
                    agent.isStopped = false;
                    etat = Etat.Errance;
                    minuterie = Random.Range(pauseMin, pauseMax);
                }
                break;
        }

        animator.SetBool("walking", etat == Etat.Errance && agent.velocity.magnitude > 0.1f);
    }

    // --- Errance : attend un peu, puis marche vers un point au hasard autour de son point de départ ---
    void Errer()
    {
        bool arrive = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        if (!arrive) return;

        minuterie -= Time.deltaTime;
        if (minuterie > 0f) return;

        Vector3 cible = origine + Random.insideUnitSphere * rayonErrance;
        if (NavMesh.SamplePosition(cible, out NavMeshHit hit, rayonErrance, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        minuterie = Random.Range(pauseMin, pauseMax);
    }

    void SortirArme()
    {
        agent.isStopped = true;
        agent.ResetPath();
        if (arme != null) arme.SetActive(true);
        animator.SetBool("armed", true);
        etat = Etat.SortieArme;
        minuterie = dureeSortieArme;
    }

    void RangerArme()
    {
        animator.SetBool("armed", false);
        etat = Etat.RangementArme;
        minuterie = dureeRangerArme;
    }

    void RegarderJoueur()
    {
        Vector3 direction = joueur.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion cible = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, cible, 8f * Time.deltaTime);
    }

    Vector3 CibleVisee() => joueur.position + Vector3.up * hauteurVisee;

    bool VoitJoueur()
    {
        Vector3 depart = pointDeTir.position;
        Vector3 direction = CibleVisee() - depart;
        if (Physics.Raycast(depart, direction.normalized, out RaycastHit hit, direction.magnitude + 1f,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == joueur || hit.transform.IsChildOf(joueur);
        }
        return true;
    }

    void Tirer()
    {
        if (balle == null || pointDeTir == null) return;

        Vector3 direction = (CibleVisee() - pointDeTir.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction)
                            * Quaternion.Euler(Random.Range(-dispersion, dispersion), Random.Range(-dispersion, dispersion), 0f);

        GameObject b = Instantiate(balle, pointDeTir.position, rotation);

        // La balle ignore les colliders de l'ennemi qui tire (sinon elle se bloque dans son corps)
        foreach (Collider cBalle in b.GetComponentsInChildren<Collider>())
            foreach (Collider cEnnemi in mesColliders)
                if (cEnnemi != null) Physics.IgnoreCollision(cBalle, cEnnemi);

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = b.transform.forward * vitesseBalle;

        Destroy(b, 5f); // Nettoie les balles perdues
    }

    // Affiche les zones dans la vue Scene quand l'ennemi est sélectionné
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rayonAggro);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? origine : transform.position, rayonErrance);
    }
}
