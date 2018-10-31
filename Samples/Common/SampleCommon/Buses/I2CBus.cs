﻿using RaspberrySharp.IO.InterIntegratedCircuit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SampleCommon.Buses
{
    /// <summary>
    /// Classe astratta ereditata da tutti i service I2C.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract class I2CBus<TKey> : IDisposable
    {
        #region Fields
        private I2cDeviceConnection _deviceConnection;
        private readonly int _deviceId;
        private Dictionary<TKey, int> _constants { get; set; }
        public CancellationTokenSource _cancellationTokenSource;
        #endregion

        #region Properties
        public bool Enabled { get; protected set; }
        #endregion

        #region Constructors
        public I2CBus(int deviceId)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            this._deviceId = deviceId;
            _constants = new Dictionary<TKey, int>();

            foreach (TKey element in Enum.GetValues(typeof(TKey)))
            {
                _constants.Add(element, Convert.ToInt32(element));
            }
        }
        #endregion

        #region Methods
        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
                Stop();
            _deviceConnection = null;
            _constants = null;
        }

        public virtual bool InitSensor()
        {
            try
            {
                if (_deviceConnection == null)
                    _deviceConnection = I2CConnectorDriver.Driver.Connect(_deviceId);

                return _deviceConnection != null;
            }
            catch (Exception e)
            {
                //CommonHelper.Logger.Error(e, string.Format("Error during Init I2C {0}:{1}", _deviceId, e.Message));
            }

            return false;
        }
        private byte GetConstantAsByte(TKey key)
        {
            _constants.TryGetValue(key, out int value);
            return (byte)value;
        }
        private string GetConstantAsString(TKey key)
        {
            _constants.TryGetValue(key, out int value);
            return "0x" + value.ToString("X").PadLeft(2, '0');
        }
        private string GetAsHexString(uint value)
        {
            return "0x" + value.ToString("X").PadLeft(2, '0');
        }
        private string GetAsHexString(int value)
        {
            return "0x" + value.ToString("X").PadLeft(2, '0');
        }
        public virtual void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        //public abstract void Start(CancellationTokenSource cancellationTokenSource);
        public virtual void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Writes one byte to i2c bus
        /// </summary>
        /// <param name="data">The byte</param>
        public void WriteRegisterByte(byte data)
        {
            try
            {
                if (_deviceConnection != null)
                    _deviceConnection.WriteByte(data);
            }
            catch (Exception e)
            {
                //CommonHelper.Logger.Error(e, e.Message);
            }
        }

        /// <summary>
        /// Write data array of bute to i2c bus
        /// </summary>
        /// <param name="data">the array of bytes</param>
        public void WriteRegisterByte(byte[] data)
        {
            try
            {
                if (_deviceConnection != null)
                    _deviceConnection.Write(data);
            }
            catch (Exception e)
            {
                //CommonHelper.Logger.Error(e, e.Message);
            }
        }
        /// <summary>
        /// Writes an integer value into register
        /// </summary>
        /// <param name="register">The register</param>
        /// <param name="data">The integer value</param>
        public void WriteRegisterByte(TKey register, ushort data)
        {
            byte[] byteArray = BitConverter.GetBytes(data).ToArray();
            this.WriteRegisterByte(register, byteArray);
        }

        /// <summary>
        /// Writes a byte value into register
        /// </summary>
        /// <param name="register">The register</param>
        /// <param name="data">The byte</param>
        public void WriteRegisterByte(TKey register, byte data)
        {
            WriteRegisterByte(register, new[] { data });
        }

        /// <summary>
        /// Writes an array of byte into register
        /// </summary>
        /// <param name="register">The register</param>
        /// <param name="data">The array of byte</param>
        public void WriteRegisterByte(TKey register, byte[] data)
        {
            if (_deviceConnection != null)
            {
                try
                {
                    byte[] command = new byte[data.Length + 1];
                    data.CopyTo(command, 1);
                    command[0] = GetConstantAsByte(register);
                    Console.WriteLine("I2C command: {0}", BitConverter.ToString(command));
                    _deviceConnection.Write(command);
                }
                catch (Exception e)
                {
                    //CommonHelper.Logger.Error(e, e.Message);
                }
            }

        }

        /// <summary>
        /// Read a register and return on byte
        /// </summary>
        /// <param name="register">The register</param>
        /// <returns>read value</returns>
        public byte ReadRegisterByte(TKey register, bool useRepeatedStart = false)
        {
            var result = ReadRegisterByte(register, 1, useRepeatedStart);
            return result != null ? result[0] : (byte)0x00;
        }

        /// <summary>
        /// Read lenght data from register
        /// </summary>
        /// <param name="register">The register to read</param>
        /// <param name="length">Number of bytes to read</param>
        /// <param name="useRepeatedStart">Use or not I2C Repeated start condition</param>
        /// <returns></returns>
        public byte[] ReadRegisterByte(TKey register, int length, bool useRepeatedStart = false)
        {
            try
            {
                if (_deviceConnection != null)
                {
                    if (useRepeatedStart)
                    {
                        byte[] res = _deviceConnection.Read(GetConstantAsByte(register), length);
                        //if (length == 2)
                        //    CommonHelper.Logger.Info(string.Format("ReadRegisterByte: {0}-{1}", res[0], res[1]));
                        return res;
                    }
                    else
                    {
                        _deviceConnection.WriteByte(GetConstantAsByte(register));
                        return _deviceConnection.Read(length);
                    }
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #endregion
    }
}
