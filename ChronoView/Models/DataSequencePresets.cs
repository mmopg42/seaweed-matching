using System.Collections.Generic;

namespace ChronoView.Models
{
    /// <summary>
    /// Predefined data sequence configurations (presets)
    /// </summary>
    public static class DataSequencePresets
    {
        /// <summary>
        /// Default preset: Normal → NIR → Cameras
        /// </summary>
        public static DataSequenceSettings NormalFirst()
        {
            return new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem
                    {
                        Type = DataType.Normal,
                        Order = 1,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 1,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.NIR,
                        Order = 2,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 1,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam1,
                        Order = 3,
                        MinDelaySeconds = 4,
                        MaxDelaySeconds = 6,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam2,
                        Order = 4,
                        MinDelaySeconds = 1,
                        MaxDelaySeconds = 2,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam3,
                        Order = 5,
                        MinDelaySeconds = 1,
                        MaxDelaySeconds = 2,
                        Enabled = true
                    }
                }
            };
        }

        /// <summary>
        /// NIR-first preset: NIR → Normal → Cameras
        /// </summary>
        public static DataSequenceSettings NirFirst()
        {
            return new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem
                    {
                        Type = DataType.NIR,
                        Order = 1,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 0,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Normal,
                        Order = 2,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 1,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam1,
                        Order = 3,
                        MinDelaySeconds = 4,
                        MaxDelaySeconds = 6,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam2,
                        Order = 4,
                        MinDelaySeconds = 1,
                        MaxDelaySeconds = 2,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam3,
                        Order = 5,
                        MinDelaySeconds = 1,
                        MaxDelaySeconds = 2,
                        Enabled = true
                    }
                }
            };
        }

        /// <summary>
        /// Cameras-first preset: Cameras → Normal → NIR
        /// </summary>
        public static DataSequenceSettings CamerasFirst()
        {
            return new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem
                    {
                        Type = DataType.Cam1,
                        Order = 1,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 0,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam2,
                        Order = 2,
                        MinDelaySeconds = 1,
                        MaxDelaySeconds = 2,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam1,
                        Order = 3,
                        MinDelaySeconds = 4,
                        MaxDelaySeconds = 9,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam2,
                        Order = 4,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 1,
                        Enabled = true
                    },
                    new DataSequenceItem
                    {
                        Type = DataType.Cam3,
                        Order = 5,
                        MinDelaySeconds = 0,
                        MaxDelaySeconds = 1,
                        Enabled = true
                    }
                }
            };
        }
    }
}
