# UrbanTaxi

**Autori:** Salvatore Alberti, Michele Quaglia

---

## 1. Panoramica e Obiettivo Principale
Il progetto **UrbanTaxi** consiste nello sviluppo in ambiente **Unity3D** di un agente autonomo basato su tecniche di **Reinforcement Learning (RL)**, progettato per navigare in scenari urbani complessi. L'obiettivo principale è la **gestione dinamica del traffico**: l'agente (taxi) deve essere in grado di reagire in tempo reale a imprevisti costanti (code, incidenti, chiusure stradali) per scegliere sempre il percorso più efficiente.

## 2. L'Ambiente di Simulazione
L'agente opera in una città simulata, concepita come un ambiente "ostile e dinamico" per testare a fondo le sue capacità di adattamento. Tra gli elementi di disturbo generati probabilisticamente troviamo:
*   **Veicoli guasti/Incidenti:** (5% di probabilità, durata 20 secondi).
*   **Traffico dinamico:** Veicoli autonomi che si muovono casualmente per la città.
*   **Blocchi stradali:** Generati in punti randomici (durata 30 secondi).

## 3. Architettura dell'Agente
L'architettura del sistema è divisa in due componenti principali che comunicano costantemente tra loro scambiandosi stati e azioni:
*   **Il Cervello (L'Agente RL):** È il responsabile delle decisioni strategiche. In base alle osservazioni dell'ambiente e al sistema di reward, sceglie il nodo stradale (incrocio) successivo per ottimizzare il percorso.
*   **Il Corpo (Il Controller):** Gestisce il movimento fisico della vettura, utilizza sensori per rilevare ostacoli e gestisce l'attraversamento materiale degli incroci.

## 4. Missione, Osservazioni e Azioni
La missione del taxi è suddivisa in due fasi: raggiungere il punto di *spawn* in cui compare una persona e, successivamente, portarla alla destinazione designata. 
L'agente non possiede una mappa globale del traffico, ma agisce basandosi su **informazioni locali**. Ad ogni incrocio, effettua un'analisi delle vie d'uscita osservando per ogni possibile scelta:
1.  La direzione della strada.
2.  Il potenziale miglioramento in termini di distanza normalizzata verso il *goal*.
3.  Il rischio di blocchi o congestioni stradali, rilevato tramite lo spherecast che ritorna 1 se in una strada colpisce una macchina ferma o un ostacolo, 0 altrimenti.

## 5. Il Sistema di Reward
Il comportamento dell'agente è plasmato da una precisa funzione di ricompensa (Reward):
*   **Guadagni (+):** Vengono assegnati punti per l'avanzamento verso la destinazione e un cospicuo premio finale (+10) al raggiungimento del goal.
*   **Penalità (-):** Vengono sottratti punti per il tempo trascorso (-0.003 al secondo), per l'allontanamento dalla meta, se si sceglie di tornare nel nodo appena lasciato (loop) e, in misura variabile, se si scelgono strade congestionate.

## 6. Addestramento (Training)
L'agente è stato addestrato per un totale di **200.000 step**, puntando a massimizzare la reward in un percorso che lo ha portato da una *policy* di scelte puramente casuali a una strategia altamente ottimizzata.

## 7. Analisi dei Risultati
I risultati dell'addestramento mostrano un'evoluzione eccellente:
*   **Cumulative Reward:** Da valori bassi e instabili della fase iniziale (0-50k step), la reward è cresciuta fino a stabilizzarsi su valori alti nella fase finale (120k-200k step), indicando un comportamento consistente.
*   **Durata degli Episodi:** Si è passati da episodi esplorativi molto lunghi (~70 decisioni) a percorsi brevi, diretti e stabili (~20/25 decisioni).
*   **Metriche di Loss:** La *Value Loss* (errore nella stima della reward futura) si è ridotta indicando stime accurate, mentre la *Policy Loss* si è stabilizzata dopo la necessaria fase di esplorazione iniziale.

## Conclusione
Il progetto UrbanTaxi ha prodotto un agente **affidabile** e intelligente. Ha imparato a prevenire i loop (evitando di tornare sui propri passi), ha sviluppato un *decision making* bilanciato (soppesando il rischio di una strada bloccata rispetto alla vicinanza al goal) e dimostra un eccellente **adattamento dinamico**, reagendo istantaneamente ai blocchi stradali e modificando il percorso in tempo reale per garantire la massima efficienza.

