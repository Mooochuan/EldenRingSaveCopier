using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EldenRingSaveCopy.Saves.Model
{
    public class SaveGame: ISaveGame
    {
        public const int SLOT_START_INDEX = 0x310;
        public const int SLOT_LENGTH = 0x280000;
        public const int SAVE_HEADERS_SECTION_START_INDEX = 0x19003B0;
        public const int SAVE_HEADERS_SECTION_LENGTH = 0x60000;
        public const int SAVE_HEADER_START_INDEX = 0x1901D0E;
        public const int SAVE_HEADER_LENGTH = 0x24C;
        public const int CHAR_ACTIVE_STATUS_START_INDEX = 0x1901D04;

        private const int CHAR_NAME_LENGTH = 0x22;
        private const int CHAR_LEVEL_LOCATION = 0x22;
        private const int CHAR_PLAYED_START_INDEX = 0x26;
        


        private bool active;
        private string characterName;
        private byte[] saveData;
        private byte[] headerData;
        private Guid id;
        private int index;

        public SaveGame()
        {
            this.active = false;
            this.characterName = string.Empty;
            this.saveData = new byte[0];
            this.id = Guid.NewGuid();
            this.index = -1;
        }

        public int Index
        {
            get => this.index;
        }

        public bool Active
        {
            get => this.active;
            set => this.active = value;
        }

        public string CharacterName
        {
            get => this.characterName;
            set => this.characterName = value ?? string.Empty;
        }

        public byte[] SaveData
        {
            get => this.saveData;
        }

        public byte[] HeaderData
        {
            get => this.headerData;
        }

        public Guid Id
        {
            get => this.id;
        }

        public int CharacterLevel { get; set; }
        public int SecondsPlayed { get; set; }

        public bool LoadData(byte[] data, int slotIndex)
        {
            try
            {
                this.index = slotIndex;
                
                // Debug logging
                string debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "save_debug.txt");
                using (StreamWriter writer = new StreamWriter(debugPath, true))
                {
                    writer.WriteLine($"\nLoading slot {slotIndex}:");
                    writer.WriteLine($"File size: {data.Length} bytes");
                    
                    // Check active status
                    byte activeStatus = data.Skip(CHAR_ACTIVE_STATUS_START_INDEX).ToArray()[0 + slotIndex];
                    writer.WriteLine($"Active status at {CHAR_ACTIVE_STATUS_START_INDEX + slotIndex}: {activeStatus}");
                    this.active = activeStatus == 1;
                    
                    // Check character name
                    byte[] nameBytes = data.Skip(SAVE_HEADER_START_INDEX + (slotIndex * SAVE_HEADER_LENGTH)).Take(CHAR_NAME_LENGTH).ToArray();
                    string rawName = Encoding.Unicode.GetString(nameBytes);
                    writer.WriteLine($"Raw name bytes: {BitConverter.ToString(nameBytes)}");
                    writer.WriteLine($"Decoded name: {rawName}");
                    this.characterName = rawName;
                    
                    // Check level
                    byte level = data.Skip(SAVE_HEADER_START_INDEX + (slotIndex * SAVE_HEADER_LENGTH)).ToArray()[CHAR_LEVEL_LOCATION];
                    writer.WriteLine($"Level at {SAVE_HEADER_START_INDEX + (slotIndex * SAVE_HEADER_LENGTH) + CHAR_LEVEL_LOCATION}: {level}");
                    this.CharacterLevel = level;
                }

                this.SecondsPlayed = BitConverter.ToInt32(data.Skip(SAVE_HEADER_START_INDEX + (slotIndex * SAVE_HEADER_LENGTH) + CHAR_PLAYED_START_INDEX).Take(4).ToArray(), 0);
                this.saveData = data.Skip(SLOT_START_INDEX + (slotIndex * 0x10) + (slotIndex * SLOT_LENGTH)).Take(SLOT_LENGTH).ToArray();
                this.headerData = data.Skip(SAVE_HEADER_START_INDEX + (slotIndex * SAVE_HEADER_LENGTH)).Take(SAVE_HEADER_LENGTH).ToArray();
                return true;
            }
            catch (Exception ex)
            {
                // Log any errors
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "save_error.txt"), 
                    $"Error loading slot {slotIndex}: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}
