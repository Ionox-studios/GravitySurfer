using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
   public void Playgame()
   {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
   }

   public void MuteCity()
   {
        SceneManager.LoadScene("1_Mute_City");
   }
   public void EbisuDesert()
   {
        SceneManager.LoadScene("2_Ebisu_Desert");
   }
   public void Singularity()
   {
        SceneManager.LoadScene("3_Event_Horizon");
   }

   public void QuitGame()
   {
    Application.Quit();
   }
   
   public void BacktoMenu()
   {
        SceneManager.LoadScene("Menu");
   }
   
}
