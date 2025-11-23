using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
//     FadeInOut fade; //xxxx
   public void Playgame()
   {
//        fade = FindAnyObjectOfType<FadeInOut>(); //
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
   }
      public void Credits()
   {
        SceneManager.LoadScene("credits");
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
