using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;

namespace SmartHomeSystem.Services
{
    public class AlarmService
    {
        private readonly AppDbContext _context;
        private static DateTime? _alarmStartTime;  // Make this static

        public AlarmService(AppDbContext context)
        {
            _context = context;
        }

        public void StopAlarm()
        {
            AlarmWorker.StopAlarm();
            _alarmStartTime = null;
        }

        public bool IsAlarmPlaying()
        {
            var proc = AlarmWorker.ActiveAlarmProcess;
            var isPlaying = proc != null && !proc.HasExited;
            //var isPlaying = true;


            if (isPlaying && !_alarmStartTime.HasValue)
            {
                _alarmStartTime = DateTime.UtcNow;
            }
            else if (!isPlaying)
            {
                _alarmStartTime = null;
            }
            
            return isPlaying;
        }

        public TimeSpan GetAlarmPlayDuration()
        {
            if (_alarmStartTime == null || !this.IsAlarmPlaying())
            {
                return TimeSpan.Zero;
            }
            
            return DateTime.UtcNow - _alarmStartTime.Value;
        }

        public Alarm GetAlarm(int id)
        {
            return _context.Alarms.Find(id);
        }

        public void UpdateAlarm(Alarm alarm)
        {
            var existingAlarm = _context.Alarms.Find(alarm.Id);
            if (existingAlarm != null)
            {
                existingAlarm.Time = alarm.Time;
                existingAlarm.Label = alarm.Label;
                existingAlarm.RepeatDaily = alarm.RepeatDaily;
                _context.SaveChanges();
            }
        }

        public List<Alarm> GetAllAlarms()
        {
            return _context.Alarms.OrderBy(a => a.Time).ToList();
        }

        public void AddAlarm(Alarm alarm)
        {
            _context.Alarms.Add(alarm);
            _context.SaveChanges();
        }

        public void DeleteAlarm(int id)
        {
            var alarm = _context.Alarms.Find(id);
            if (alarm != null)
            {
                // Stop any currently playing alarm
                StopAlarm();
                
                // Remove from database
                _context.Alarms.Remove(alarm);
                _context.SaveChanges();
            }
        }
    }
}