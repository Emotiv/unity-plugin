using System;
using System.Collections.Generic;
using System.Collections;
 
    public class BufferStream
    {
        public ArrayList _buf = null;
        int _winSize = 0;
        int _stepSize = 0;
        int _curPos = 0;
        const int BUFFER_SIZE = 1024;

        static readonly object _object = new object();

        public BufferStream(int windowSize, int stepSize)
        {
            _buf      = new ArrayList();
            _winSize  = windowSize;
            _stepSize = stepSize;
            _curPos   = 0;
        }

        public int WindowSize
        {
            get {
                return _winSize;
            }
            set {
                _winSize = value;
            }
        }

        public int StepSize
        {
            get {
                return _stepSize;
            }
            set {
                _stepSize = value;
            }
        }

        void processOverFlow()
        {
            if (_buf.Count <= 0)
                return;

            int overflow = _buf.Count - BUFFER_SIZE;
            if (overflow > 0) {
                _buf.RemoveRange(0, overflow);
                _curPos -= overflow;
                if(_curPos < 0)
                    _curPos = 0;
            }
        }

        public void AppendData(double data)
        {
            lock (_object)
            {
                _buf.Add(data);
                processOverFlow();
            }
        }
        
        public void AppendData(double[] data)
        {
            lock (_object)
            {
                _buf.AddRange(data);
                processOverFlow();
            }
        }

        // Get the next segment base on window size and step size
        // Return a newly allocated array
        private double[] Next()
        {
            if (_buf.Count == 0)
			    return null;
		
		    // Not enough data for next segment, return
		    if (_curPos + _winSize > _buf.Count)
			    return null;

            // Construct segment from buffer
            double[] segment = new double[_winSize];

            _buf.CopyTo(_curPos, segment, 0, _winSize);

            // Increment current position by step size
            _curPos += _stepSize;

		    // Return segment
            return segment;
        }        

        public double[] NextWithRemoval()
        {
            lock (_object)
            {
                double[] segment = Next();

                if (segment == null)
                    return segment;

                if (_buf.Count == _stepSize) {
                    _buf = null;
                    _buf = new ArrayList();
                }
                else {
                    _buf.RemoveRange(0, _curPos);
                }
                
                ResetCurrentPosition();
                return segment;
            }
        }

        private void ResetCurrentPosition()
        {
            _curPos = 0;
        }

        public void Reset()
        {
            lock (_object)
            {
                _buf = null;
                _buf = new ArrayList();
                ResetCurrentPosition();
            }
        }

        public void ViewBuffer()
        {
            int i = 0;
            double ss = 0;
            string s = string.Empty;
            while (0==0)
            {
                try
                {
                    ss += (double)_buf[i];
                    s += _buf[i].ToString().Substring(0, 4) + " ";
                    i++;
                }
                catch (System.ArgumentOutOfRangeException)
                {
                	break;
                }
            }
            ss /= i;
            UnityEngine.Debug.Log(s);
            UnityEngine.Debug.Log(ss);
        }

        public int GetBufSize()
        {
            lock (_object)
            {
                return _buf.Count;
            }
        }
    }

