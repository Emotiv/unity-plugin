package com.emotiv.unityplugin;

import android.app.Service;
import android.content.Intent;
import android.os.IBinder;
import android.util.Log;

public class BForegroundService extends Service {

    public static final String CHANNEL_ID = "BForegroundServiceChannel";
    private static final String TAG = "MyBackgroundService";
    // private static final int START_ID = 6869;

    @Override
    public void onCreate() {
        super.onCreate();
        Log.d(TAG, "Service Created");
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
		
        Log.d(TAG, "Service Started");
        // Perform your background tasks here
        return START_STICKY;

        // String action = new String(intent.getByteArrayExtra("action"));
        // if (action.equals("Start")) {
        //     String contentText = new String(intent.getByteArrayExtra("contentText"));
		// 	String appName = new String(intent.getByteArrayExtra("appName"));

        //     Intent notificationIntent = new Intent(this, MyActivity.class);
        //     PendingIntent pendingIntent = PendingIntent.getActivity(this,
        //             0, notificationIntent, PendingIntent.FLAG_IMMUTABLE);
        //     int colorBg = Color.argb(1, 1, 1, 1);
        //     Notification notification = new NotificationCompat.Builder(this, CHANNEL_ID)
        //             .setContentTitle(appName)
        //             .setContentText(contentText)
        //             .setSmallIcon(R.mipmap.ic_notification)
        //             .setColor(colorBg)
        //             .setContentIntent(pendingIntent)
        //             .setOngoing(false)
        //             .build();
		// 	if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
		// 		startForeground(START_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE);
        //     } else {
		// 		startForeground(START_ID, notification);
        //     }
        // } else if (action.equals("Stop")) {
        //     stopForeground(Service.STOP_FOREGROUND_REMOVE);
        //     stopSelf();
        // }

        // return START_NOT_STICKY;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        Log.d(TAG, "Service Destroyed");
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }
}
