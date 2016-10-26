namespace PunchServer
{
    class PunchResponse
    {
        public bool Valid { get; set; }
        public string Message { get; set; }
        public string ServerAddress { get; set; }
        public string ServerPort { get; set; }
    }
}
