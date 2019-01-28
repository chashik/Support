using Support;
using System.Net;

namespace Test
{
    internal class Employee : ApiClient
    {
        private bool _aquired;
        private Message _message;

        public Employee(string apiHost) : base(apiHost) { }

        public int Offset { get; set; }

        protected override void Work()
        {
            if (_aquired) // answer message, put it to server and wait until next time
            {
                _message.Answer = $"test answer from {Login}";

                if (Put($"api/messages/{_message.Id}", _message, out HttpStatusCode code))
                {
                    _aquired = false;
                    WriteInline($"{Login}: message answered (id: {_message.Id})");
                }
                else
                    WriteInline($"{Login}: unexpected processing result, HttpStatus: {code}");
            }
            else // aquire message from server and wait  until next time
            {
                if (Get($"api/messages/{Login}/{Offset}", out HttpStatusCode code, out _message))
                {
                    _message.OperatorId = Login;

                    if (Put($"api/messages/{_message.Id}", _message, out code))
                    {
                        _aquired = true;
                        WriteInline($"{Login}: message aquired (id: {_message.Id})");
                    }
                    else
                        WriteInline($"{Login}: Unexpected updating result, HttpStatus: {code}");
                }
                else
                    WriteInline($"{Login}: Not aquired, HttpStatus: {code}");
            }
        }
    }
}
