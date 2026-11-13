// The little behaviour the pages need. Everything else is server rendered.
(function () {
    "use strict";

    // Collapsing navigation on narrow screens.
    var toggle = document.querySelector(".nav-toggle");
    var nav = document.querySelector(".site-nav");

    if (toggle && nav) {
        toggle.addEventListener("click", function () {
            var open = nav.classList.toggle("open");
            toggle.setAttribute("aria-expanded", String(open));
        });
    }

    // The upload form needs the length of the video, which only the browser
    // knows before the file is sent. The server validates it again.
    var fileInput = document.querySelector("[data-video-input]");
    var durationInput = document.querySelector("[data-video-duration]");
    var durationNote = document.querySelector("[data-duration-note]");

    if (fileInput && durationInput) {
        fileInput.addEventListener("change", function () {
            var file = fileInput.files && fileInput.files[0];

            if (!file) {
                durationInput.value = "";
                return;
            }

            var probe = document.createElement("video");
            probe.preload = "metadata";
            probe.onloadedmetadata = function () {
                var seconds = Math.round(probe.duration || 0);
                durationInput.value = seconds > 0 ? seconds : "";

                if (durationNote) {
                    durationNote.textContent = seconds > 0
                        ? "Length read: " + seconds + " s"
                        : "The length could not be read from this file.";
                }

                URL.revokeObjectURL(probe.src);
            };
            probe.src = URL.createObjectURL(file);
        });
    }
})();
