using UnityEngine;
using System.Collections.Generic;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class TaxiAgent : Agent
{
    private float tempoEpisodio;
    private float tempoStalloAccumulato;
    private bool inAttesaDecisione;

    private TaxiController taxi;

    public override void OnEpisodeBegin()
    {
        // Reset timer dell'episodio
        tempoEpisodio = 0f;

        // Reset stallo accumulato
        tempoStalloAccumulato = 0f;

        // Reset flag
        inAttesaDecisione = false;
    }

    public override void Initialize()
    {
        taxi = GetComponent<TaxiController>();
    }

    public override void CollectObservations (VectorSensor sensor)
    {
        //Recuperiamo le uscite disponibili ad un incrocio
        List<RoadExit> uscite = taxi.GetUsciteDisponibili();
        int before = sensor.ObservationSize();

        //Per ogni uscita possibile (Abbaimo un massimo di 3 uscite per incrocio)
        for(int i=0; i<3; i++)
        {
            if (i < uscite.Count)
            {

                RoadExit uscita = uscite[i];

                //Distanza dal nodo obiettivo passando per questa strada
                sensor.AddObservation(uscita.DistanzaDallGoal);
                
                //Torna indietro?
                sensor.AddObservation(uscita.TornaIndietro ? 1f : 0f);

                //Lunghezza della strada collegata
                sensor.AddObservation(uscita.LunghezzaStrada);

                //Rallentamenti percepiti
                sensor.AddObservation(uscita.Rallentamenti);

            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        //feature globali
        
        //Velocità media del taxi
        sensor.AddObservation(taxi.velocitaMedia);

        //Tempo Fermo
        sensor.AddObservation(taxi.TempoFermo);

        //Distanza residua dal goal
        sensor.AddObservation(taxi.DistanzaDallGoal);
                
        //Tempo trascorso
        sensor.AddObservation(taxi.TempoTrascorso);
     
        Debug.Log($"OBS COUNT: {sensor.ObservationSize() - before}");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Azione scelta dal modello (0, 1 o 2)
        int azioniIndex = actions.DiscreteActions[0];

        // Recupera le uscite disponibili all'incrocio
        List<RoadExit> uscite = taxi.GetUsciteDisponibili();

        // Controllo di sicurezza per verificare se un'azione è valida
        if (azioniIndex < 0 || azioniIndex >= uscite.Count)
        {
            // Azione non valida
            azioniIndex = 0;
        }

        // Seleziona l'uscita scelta
        RoadExit uscitaSelezionata = uscite[azioniIndex];

        // Comunica la scelta al TaxiController
        taxi.SetUscitaSelezionata(uscitaSelezionata);
    }


    void FixedUpdate()
    {
        tempoEpisodio += Time.fixedDeltaTime;
        PenalitaTemporale();
    }

    void PenalitaTemporale()
    {
        // Penalità costante per il tempo che passa
        AddReward(-0.001f);
    }

    public void NotificaStallo(float durataStallo)
    {
        tempoStalloAccumulato = tempoStalloAccumulato + durataStallo;
        AddReward(-0.01f * durataStallo);
    }

    public void NotificaDestinazioneRaggiunta()
    {
        AddReward(+1.0f);
        EndEpisode();
    }

}
