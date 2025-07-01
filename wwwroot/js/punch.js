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

    let dateTimeInterval;
    let enteredValue = '';

    function toggleSubmitButton() {
        submitBtn.disabled = enteredValue.trim().length === 0;
    }

    function updateDateTime() {
        const now = new Date();
        dateTimeDiv.textContent = now.toLocaleString();
    }

    function startClock() {
        updateDateTime();
        dateTimeInterval = setInterval(updateDateTime, 1000);
    }

    function stopClock() {
        clearInterval(dateTimeInterval);
    }

    function resetUI() {
        messageDiv.textContent = '';
        messageDiv.style.color = '';
        dateTimeDiv.textContent = '';
        snapshot.style.display = 'none';
        video.style.display = 'block';
        enteredValue = '';
        numInput.value = '';
        numInput.style.display = 'block';
        numpadDiv.style.display = 'grid';
        submitBtn.style.display = 'inline-block';
        submitBtn.disabled = true;
        startClock();
    }


//fullscreen logic goes here



    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        navigator.mediaDevices.getUserMedia({ video: true })
            .then(stream => {
                video.srcObject = stream;
                video.play();
            })
            .catch(err => {
                messageDiv.textContent = 'Camera access denied: ' + err.message;
                messageDiv.style.color = 'red';
            });
    } else {
        messageDiv.textContent = 'Camera not supported in this browser.';
        messageDiv.style.color = 'red';
    }

    startClock();

    document.querySelectorAll('.num-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            if (enteredValue.length >= 20) return;
            enteredValue += btn.textContent;
            numInput.value = '*'.repeat(enteredValue.length);
            messageDiv.textContent = '';
            messageDiv.style.color = '';
            snapshot.style.display = 'none';
            toggleSubmitButton();
        });
    });

    document.getElementById('clear-btn').addEventListener('click', () => {
        enteredValue = '';
        numInput.value = '';
        messageDiv.textContent = '';
        messageDiv.style.color = '';
        snapshot.style.display = 'none';
        toggleSubmitButton();
    });

    document.getElementById('backspace-btn').addEventListener('click', () => {
        enteredValue = enteredValue.slice(0, -1);
        numInput.value = '*'.repeat(enteredValue.length);
        messageDiv.textContent = '';
        messageDiv.style.color = '';
        snapshot.style.display = 'none';
        toggleSubmitButton();
    });

    function capturePhoto() {
        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth || 320;
        canvas.height = video.videoHeight || 240;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        return canvas.toDataURL('image/png');
    }

    submitBtn.addEventListener('click', async () => {
        stopClock();
        const employeeId = enteredValue.trim();
        if (!employeeId) {
            alert('Please enter employee ID');
            return;
        }

        const punchTimeObj = new Date();
        const punchTime = punchTimeObj.toISOString();
        const photoData = capturePhoto();

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
                snapshot.style.display = 'none';
                errorSound.play();
                setTimeout(resetUI, 2000);
                return;
            }

            const result = await response.json();
            successSound.play();
            dateTimeDiv.textContent = punchTimeObj.toLocaleString();
            video.style.display = 'none';
            snapshot.src = photoData;
            snapshot.style.display = 'block';
            messageDiv.innerHTML = `<span style="color:green; font-weight:bold;">Punch successful! Employee: ${result.employeeInitials}</span>`;

            numInput.style.display = 'none';
            numpadDiv.style.display = 'none';
            submitBtn.style.display = 'none';

            setTimeout(resetUI, 2000);
        } catch (err) {
            errorSound.play();
            messageDiv.textContent = 'Network or server error: ' + err.message;
            messageDiv.style.color = 'red';
            snapshot.style.display = 'none';
            setTimeout(resetUI, 2000);
        }
    });
});
