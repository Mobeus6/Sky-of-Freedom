package com.iliadstudio.skyoffreedom.auth;

import android.app.Activity;
import android.os.CancellationSignal;
import android.os.Handler;
import android.os.Looper;
import androidx.annotation.Keep;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.NoCredentialException;
import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;
import org.json.JSONObject;

@Keep
public final class GoogleSignInBridge {
    private static final Handler MAIN = new Handler(Looper.getMainLooper());
    private static Pending active;

    private GoogleSignInBridge() {}

    private static final class Pending {
        final String receiver;
        final String id;
        final CancellationSignal cancellation = new CancellationSignal();
        Runnable timeout;

        Pending(String receiver, String id) {
            this.receiver = receiver;
            this.id = id;
        }
    }

    @Keep
    public static void begin(String receiver, String requestId, String webClientId) {
        MAIN.post(() -> {
            if (active != null) {
                send(receiver, requestId, "busy", "");
                return;
            }

            Activity activity = UnityPlayer.currentActivity;
            if (activity == null || activity.isFinishing()) {
                send(receiver, requestId, "error", "");
                return;
            }

            final Pending pending = new Pending(receiver, requestId);
            active = pending;
            pending.timeout = () -> {
                if (active == pending) {
                    complete(pending, "timeout", "");
                    pending.cancellation.cancel();
                }
            };
            MAIN.postDelayed(pending.timeout, 120000);

            try {
                GetSignInWithGoogleOption option =
                    new GetSignInWithGoogleOption.Builder(webClientId).build();
                GetCredentialRequest request =
                    new GetCredentialRequest.Builder()
                        .addCredentialOption(option)
                        .build();

                CredentialManager.create(activity).getCredentialAsync(
                    activity,
                    request,
                    pending.cancellation,
                    command -> MAIN.post(command),
                    new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                        @Override
                        public void onResult(GetCredentialResponse response) {
                            if (active != pending) return;
                            try {
                                Credential credential = response.getCredential();
                                if (!(credential instanceof CustomCredential) ||
                                    !GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
                                        .equals(credential.getType())) {
                                    complete(pending, "error", "");
                                    return;
                                }
                                String token = GoogleIdTokenCredential
                                    .createFrom(credential.getData()).getIdToken();
                                complete(pending, "success", token);
                            } catch (Exception error) {
                                complete(pending, "error", "");
                            }
                        }

                        @Override
                        public void onError(GetCredentialException error) {
                            String status = error instanceof GetCredentialCancellationException
                                ? "cancelled"
                                : error instanceof NoCredentialException
                                    ? "no_credential" : "error";
                            complete(pending, status, "");
                        }
                    }
                );
            } catch (Exception error) {
                complete(pending, "error", "");
            }
        });
    }

    @Keep
    public static void cancel(String requestId) {
        MAIN.post(() -> {
            if (active == null || !active.id.equals(requestId)) return;
            Pending pending = active;
            active = null;
            MAIN.removeCallbacks(pending.timeout);
            pending.cancellation.cancel();
        });
    }

    private static void complete(Pending pending, String status, String token) {
        if (active != pending) return;
        active = null;
        MAIN.removeCallbacks(pending.timeout);
        send(pending.receiver, pending.id, status, token);
    }

    private static void send(String receiver, String id, String status, String token) {
        try {
            JSONObject result = new JSONObject();
            result.put("requestId", id);
            result.put("status", status);
            result.put("token", token);
            UnityPlayer.UnitySendMessage(receiver, "OnGoogleCredential", result.toString());
        } catch (Exception ignored) {
            // Never log credential payloads.
        }
    }
}
