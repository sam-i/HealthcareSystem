// Utility Functions
const utils = {
    showToast: function (message, type = 'success') {
        const toast = `
            <div class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>`;
        $('#toastContainer').append(toast);
        const toastElement = $('.toast').last();
        const bsToast = new bootstrap.Toast(toastElement);
        bsToast.show();

        toastElement.on('hidden.bs.toast', function () {
            $(this).remove();
        });
    },

    getImageTypeName: function (imageType) {
        const types = {
            1: 'MRI',
            2: 'CT',
            3: 'X-Ray'
        };
        return types[parseInt(imageType)] || 'Unknown';
    },

    formatDate: function (dateString) {
        return new Date(dateString).toLocaleDateString();
    },

    formatReportDate: function (date) {
        return new Date(date).toLocaleString();
    },

    cleanupModal: function (modalId) {
        $(`#${modalId}`).modal('hide');
        $('.modal-backdrop').remove();
        $(`#${modalId} form`)[0]?.reset();
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
        $('body')
            .removeClass('modal-open')
            .css({
                'overflow': 'auto',
                'padding-right': '0'
            });
        setTimeout(() => {
            window.dispatchEvent(new Event('resize'));
        }, 10);
    }
};

// Task Management
const taskManager = {
    loadTasks: function (patientId) {
        $.get('/Doctor/GetPatientTasks', { patientId: patientId })
            .done(function (tasks) {
                // Task loading logic
            })
            .fail(function (xhr, status, error) {
                console.error('Error loading tasks:', error);
                utils.showToast('Error loading tasks', 'danger');
            });
    },

    addTask: function (formData) {
        // Add task logic
    },

    editTask: function (formData) {
        // Edit task logic
    },

    deleteTask: function (taskId, patientId) {
        // Delete task logic
    }
};

// Image Management
const imageManager = {
    loadImages: function (patientId) {
        // Image loading logic
    },

    updateImageDetails: function (formData) {
        // Update image details logic
    },

    viewImage: function (image) {
        // View image logic
    }
};

// Report Generation
const reportManager = {
    generateReport: function (reportData) {
        // Report generation logic
    },

    printReport: function () {
        // Print report logic
    }
};

// Event Handlers
$(document).ready(function () {
    // Initialize event handlers
    // ...
});