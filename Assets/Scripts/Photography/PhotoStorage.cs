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
   private readonly Dictionary<Photo, QuestionDefinition> assignedQuestions = new();

   public IReadOnlyList<Photo> Photos => photos;
   public event Action PhotosChanged;
   public bool Contains(Photo photo) => photo != null && photos.Contains(photo);
   public bool Owns(Photo photo) => photo != null && (photos.Contains(photo) || assignedQuestions.ContainsKey(photo));
   public int Capacity => maxCapacity;

   public void SetCapacity(int capacity) { maxCapacity = capacity; }

   // Can change maxCapacity to any other value depending on gameplay.
   // ( Currently 5 just because that's what was chsoen so far )
   [Tooltip("Maximum number of photos in storage. The notebook sets this from its slot count.")]
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
       int count = 0;
       foreach (var photo in photos) if (photo != null) count++;
       return count;
   }


   /// <summary>
   /// Returns 'true' if the photo storage is at maximum capacity.
   /// </summary>
   public bool IsPhotoStorageFull()
   {
       return GetPhotoCount() >= maxCapacity;
   }


   /// <summary>
   /// Adds a captured photo to the storage.
   /// If the storage is at maximum capacity, returns 'false'.
   /// </summary>
   public bool AddPhoto(Photo photo)
   {
       if (photo == null || Owns(photo) || IsPhotoStorageFull())
       {
           return false;
       }

       PutInSlot(photo, FindOpenSlot());
       photo.transform.SetParent(transform);
       PhotosChanged?.Invoke();
       return true;
   }

   private int FindOpenSlot()
   {
       for (int i = 0; i < maxCapacity; i++)
           if (i >= photos.Count || photos[i] == null) return i;
       return -1;
   }

   private void PutInSlot(Photo photo, int index)
   {
       while (photos.Count <= index) photos.Add(null);
       photos[index] = photo;
   }

   /// <summary>Moves a photo into a question, returning displaced evidence to its source.</summary>
   public bool MoveToQuestion(Photo photo, QuestionDefinition destination)
   {
       if (photo == null || destination == null || !Owns(photo)) return false;
       assignedQuestions.TryGetValue(photo, out var source);
       if (source == destination) return true;
       int storageSlot = photos.IndexOf(photo);
       Photo displaced = destination.Photo;

       if (source != null) source.AssignPhoto(displaced);
       else photos[storageSlot] = displaced;

       if (displaced != null)
       {
           if (source != null) assignedQuestions[displaced] = source;
           else assignedQuestions.Remove(displaced);
       }
       assignedQuestions[photo] = destination;
       destination.AssignPhoto(photo);
       PhotosChanged?.Invoke();
       return true;
   }

   /// <summary>Returns question evidence to an open storage slot. A full slot leaves it in place.</summary>
   public bool ReturnPhoto(Photo photo, int slot = -1)
   {
       if (photo == null || !assignedQuestions.TryGetValue(photo, out var source)) return false;
       if (slot < 0) slot = FindOpenSlot();
       if (slot < 0 || slot >= maxCapacity || (slot < photos.Count && photos[slot] != null)) return false;

       assignedQuestions.Remove(photo);
       source.AssignPhoto(null);
       PutInSlot(photo, slot);
       PhotosChanged?.Invoke();
       return true;
   }


   /// <summary>
   /// Removes a particular photo from the photo storage and deletes its
   /// respective GameObject.
   /// </summary>
   public bool RemovePhoto(Photo photo)
   {
       if (photo == null) return false;
       int slot = photos.IndexOf(photo);
       bool removed = slot >= 0 || assignedQuestions.ContainsKey(photo);
       if (slot >= 0) photos[slot] = null;
       if (assignedQuestions.TryGetValue(photo, out var question))
       {
           assignedQuestions.Remove(photo);
           question.AssignPhoto(null);
       }
       if (removed)
       {
           PhotosChanged?.Invoke();
           Destroy(photo.ImageTexture);
           Destroy(photo.gameObject);
       }
       return removed;
   }

}
