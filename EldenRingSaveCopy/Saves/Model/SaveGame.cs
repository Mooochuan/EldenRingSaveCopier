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
        // Elden Ring offsets
        public const int ER_SLOT_START_INDEX = 0x310;
        public const int ER_SLOT_LENGTH = 0x280000;
        public const int ER_SAVE_HEADERS_SECTION_START_INDEX = 0x19003B0;
        public const int ER_SAVE_HEADERS_SECTION_LENGTH = 0x60000;
        public const int ER_SAVE_HEADER_START_INDEX = 0x1901D0E;
        public const int ER_SAVE_HEADER_LENGTH = 0x24C;
        public const int ER_CHAR_ACTIVE_STATUS_START_INDEX = 0x1901D04;

        // Nightreign offsets (will be determined by scanning)
        public static int NR_SLOT_START_INDEX = 0x310;
        public static int NR_SLOT_LENGTH = 0x1A0000;
        public static int NR_SAVE_HEADERS_SECTION_START_INDEX = 0x11003B0;
        public static int NR_SAVE_HEADERS_SECTION_LENGTH = 0x40000;
        public static int NR_SAVE_HEADER_START_INDEX = 0x1101D0E;
        public static int NR_SAVE_HEADER_LENGTH = 0x24C;
        public static int NR_CHAR_ACTIVE_STATUS_START_INDEX = 0x1101D04;

        private const int CHAR_NAME_LENGTH = 0x22;
        private const int CHAR_LEVEL_LOCATION = 0x22;
        private const int CHAR_PLAYED_START_INDEX = 0x26;

        private bool active;
        private string characterName;
        private byte[] saveData;
        private byte[] headerData;
        private Guid id;
        private int index;
        public bool isNightreign { get; private set; }

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

        private static void ScanForOffsets(byte[] data)
        {
            string debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "save_scan.txt");
            using (StreamWriter writer = new StreamWriter(debugPath))
            {
                writer.WriteLine("Scanning save file for character names and patterns...");
                writer.WriteLine($"File size: {data.Length} bytes");

                // Look for Unicode character names (they start with a non-zero byte followed by 0x00)
                for (int i = 0; i < data.Length - 2; i++)
                {
                    if (data[i] != 0 && data[i + 1] == 0)
                    {
                        // Found potential start of a Unicode string
                        byte[] potentialName = new byte[CHAR_NAME_LENGTH];
                        Array.Copy(data, i, potentialName, 0, CHAR_NAME_LENGTH);
                        string name = Encoding.Unicode.GetString(potentialName).TrimEnd('\0');
                        
                        // Only log if it looks like a real name (contains letters and is not too short)
                        if (name.Length >= 2 && name.All(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)))
                        {
                            writer.WriteLine($"\nPotential character name at offset 0x{i:X}:");
                            writer.WriteLine($"Name: {name}");
                            writer.WriteLine($"Raw bytes: {BitConverter.ToString(potentialName)}");
                            
                            // Look for level byte (usually 1-2 bytes after name)
                            if (i + CHAR_NAME_LENGTH + 2 < data.Length)
                            {
                                byte level = data[i + CHAR_NAME_LENGTH + 2];
                                writer.WriteLine($"Level byte at 0x{i + CHAR_NAME_LENGTH + 2:X}: {level}");
                            }
                            
                            // Look for active status (usually a few bytes before name)
                            if (i >= 4)
                            {
                                byte activeStatus = data[i - 4];
                                writer.WriteLine($"Active status at 0x{i - 4:X}: {activeStatus}");
                            }
                        }
                    }
                }
            }
        }

        public bool LoadData(byte[] data, int slotIndex)
        {
            try
            {
                this.index = slotIndex;
                
                // Determine if this is a Nightreign save file
                isNightreign = data.Length < 20000000; // ER files are ~29MB, NR files are ~19MB

                // If this is the first slot of a Nightreign save, scan for offsets
                if (isNightreign && slotIndex == 0)
                {
                    ScanForOffsets(data);
                }

                // Select appropriate offsets based on file type
                int SLOT_START_INDEX = isNightreign ? NR_SLOT_START_INDEX : ER_SLOT_START_INDEX;
                int SLOT_LENGTH = isNightreign ? NR_SLOT_LENGTH : ER_SLOT_LENGTH;
                int SAVE_HEADER_START_INDEX = isNightreign ? NR_SAVE_HEADER_START_INDEX : ER_SAVE_HEADER_START_INDEX;
                int SAVE_HEADER_LENGTH = isNightreign ? NR_SAVE_HEADER_LENGTH : ER_SAVE_HEADER_LENGTH;
                int CHAR_ACTIVE_STATUS_START_INDEX = isNightreign ? NR_CHAR_ACTIVE_STATUS_START_INDEX : ER_CHAR_ACTIVE_STATUS_START_INDEX;

                // Debug logging
                string debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "save_debug.txt");
                using (StreamWriter writer = new StreamWriter(debugPath, true))
                {
                    writer.WriteLine($"\nLoading slot {slotIndex} from {(isNightreign ? "Nightreign" : "Elden Ring")} save:");
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
                    $"Error loading slot {slotIndex} from {(isNightreign ? "Nightreign" : "Elden Ring")} save: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}
