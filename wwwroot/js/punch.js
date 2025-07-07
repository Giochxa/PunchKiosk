document.addEventListener('DOMContentLoaded', () => {
    const video = document.getElementById('video');
    const snapshot = document.getElementById('snapshot');
    const messageDiv = document.getElementById('message');
    const dateTimeDiv = document.getElementById('dateTime');
    const numInput = document.getElementById('numInput');
    const submitBtn = document.getElementById('submitPunch');
    const numpadDiv = document.getElementById('numpad');
    const successSound = new Audio('/sounds/success.mp3');
    const errorSound = new Audio('/sounds/error.mp3');

    let enteredValue = '';
    let videoStream = null;
    let cameraAvailable = false;

    function toggleSubmitButton() {
        submitBtn.disabled = enteredValue.trim().length === 0;
    }

    function updateDateTime() {
        const now = new Date();
        dateTimeDiv.textContent = now.toLocaleString();
    }

    setInterval(updateDateTime, 1000);
    updateDateTime();

    // Start camera with 5s timeout fallback
    async function startCamera() {
        try {
            videoStream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'user' },
                audio: false
            });
            video.srcObject = videoStream;
            video.play();

            const timeout = new Promise((resolve) => {
                setTimeout(() => resolve(false), 5000);
            });

            const ready = new Promise((resolve) => {
                const checkReady = () => {
                    if (video.videoWidth > 0) resolve(true);
                    else requestAnimationFrame(checkReady);
                };
                checkReady();
            });

            cameraAvailable = await Promise.race([ready, timeout]);

            if (!cameraAvailable) {
                messageDiv.textContent = "⚠️ Camera preview not available. Punch will proceed without photo.";
                messageDiv.style.color = "orange";
            }

        } catch (err) {
            cameraAvailable = false;
            messageDiv.textContent = 'Camera error: ' + err.message;
            messageDiv.style.color = 'red';
        }
    }

    startCamera();

    function resetUI() {
        messageDiv.textContent = '';
        messageDiv.style.color = '';
        snapshot.style.display = 'none';
        snapshot.src = '';
        video.style.display = 'block';
        document.getElementById("videoContainer").style.display = "block";
        document.getElementById("snapshotContainer").style.display = "none";
        enteredValue = '';
        numInput.value = '';
        numInput.style.display = 'block';
        numpadDiv.style.display = 'grid';
        submitBtn.style.display = 'inline-block';
        toggleSubmitButton();
    }

    document.querySelectorAll('.num-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            if (enteredValue.length >= 20) return;
            enteredValue += btn.textContent;
            numInput.value = '*'.repeat(enteredValue.length);
            toggleSubmitButton();
        });
    });

    document.getElementById('clear-btn').addEventListener('click', () => {
        enteredValue = '';
        numInput.value = '';
        toggleSubmitButton();
    });

    document.getElementById('backspace-btn').addEventListener('click', () => {
        enteredValue = enteredValue.slice(0, -1);
        numInput.value = '*'.repeat(enteredValue.length);
        toggleSubmitButton();
    });

    function capturePhoto() {
        return new Promise((resolve) => {
            if (!cameraAvailable) {
                return resolve(""); // empty image if no camera
            }

            const waitForFrame = () => {
                if (video.videoWidth === 0 || video.videoHeight === 0) {
                    requestAnimationFrame(waitForFrame);
                    return;
                }

                const canvas = document.createElement('canvas');
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                resolve(canvas.toDataURL('image/png'));
            };

            video.style.display = 'block';
            document.getElementById("videoContainer").style.display = "block";
            requestAnimationFrame(waitForFrame);
        });
    }

    submitBtn.addEventListener('click', async () => {
        const employeeId = enteredValue.trim();
        if (!employeeId) return;

        const punchTimeObj = new Date();
        const punchTime = punchTimeObj.toISOString();
        const photoData = await capturePhoto();

        try {
            const response = await fetch('/api/punch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ employeeId, punchTime, photoData })
            });

            if (!response.ok) {
                const error = await response.text();
                messageDiv.textContent = 'Error: ' + error;
                messageDiv.style.color = 'red';
                errorSound.play();
                setTimeout(resetUI, 2000);
                return;
            }

            const result = await response.json();

            if (cameraAvailable) {
                snapshot.src = photoData;
                snapshot.style.display = 'block';
                document.getElementById("snapshotContainer").style.display = "block";
                document.getElementById("videoContainer").style.display = "none";
                video.style.display = "none";
            }

            messageDiv.innerHTML =
                `<span style="color:green; font-weight:bold;">Punch successful! Employee: ${result.employeeInitials}</span>`;

            numInput.style.display = 'none';
            numpadDiv.style.display = 'none';
            submitBtn.style.display = 'none';

            successSound.play();
            setTimeout(resetUI, 2000);
        } catch (err) {
            errorSound.play();
            messageDiv.textContent = 'Network or server error: ' + err.message;
            messageDiv.style.color = 'red';
            setTimeout(resetUI, 2000);
        }
    });
});
