using UnityEngine;
using System.Collections.Generic;
using System;


/// <summary>
/// Handles management of the player's captured photos.
/// Also enforces the maximum capacity of the photo storage.
/// </summary>


public class PhotoStorage : MonoBehaviour
{
   public static PhotoStorage Instance { get; private set; }
   private readonly List<Photo> photos = new List<Photo>();

   public IReadOnlyList<Photo> Photos => photos;
   public event Action PhotosChanged;
   public bool Contains(Photo photo) => photos.Contains(photo);
   public int Capacity => maxCapacity;

   public void SetCapacity(int capacity) { maxCapacity = capacity; }

   // Can change maxCapacity to any other value depending on gameplay.
   // ( Currently 5 just because that's what was chsoen so far )
   [SerializeField] private int maxCapacity = 5;


   private void Awake()
   {
       if (Instance != null && Instance != this)
       {
           Destroy(gameObject);
           return;
       }


       Instance = this;


       DontDestroyOnLoad(gameObject);


   }




   /// <summary>
   /// Returns the number of photos currently held in the storage.
   /// </summary>
   public int GetPhotoCount()
   {
       return photos.Count;
   }


   /// <summary>
   /// Returns 'true' if the photo storage is at maximum capacity.
   /// </summary>
   public bool IsPhotoStorageFull()
   {
       return photos.Count >= maxCapacity;
   }


   /// <summary>
   /// Adds a captured photo to the storage.
   /// If the storage is at maximum capacity, returns 'false'.
   /// </summary>
   public bool AddPhoto(Photo photo)
   {
       if (photo == null || photos.Contains(photo) || IsPhotoStorageFull())
       {
           return false;
       }

       photos.Add(photo);
       photo.transform.SetParent(transform);
       PhotosChanged?.Invoke();
       return true;
   }


   /// <summary>
   /// Removes a particular photo from the photo storage and deletes its
   /// respective GameObject.
   /// </summary>
   public bool RemovePhoto(Photo photo)
   {
       bool removed = photos.Remove(photo);
       if (removed)
       {
           PhotosChanged?.Invoke();
           Destroy(photo.ImageTexture);
           Destroy(photo.gameObject);
       }
       return removed;
   }

}
