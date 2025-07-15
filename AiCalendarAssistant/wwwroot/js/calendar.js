document.addEventListener('DOMContentLoaded', function () {
    let calendar;
    let selectedEvent = null;
    let selectedTask = null;
    calendar = initializeCalendar(); // ✅ assign to the global variable
    loadAllItems(); // ✅ now calendar is defined

    // Navigation controls
    document.getElementById('prevBtn').addEventListener('click', () => {
        calendar.prev();
    });

    document.getElementById('nextBtn').addEventListener('click', () => {
        calendar.next();
    });

    document.getElementById('todayBtn').addEventListener('click', () => {
        calendar.today();
    });

    // View switching
    document.querySelectorAll('.view-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const view = this.dataset.view;
            calendar.changeView(view);

            // Update active button
            document.querySelectorAll('.view-btn').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
        });
    });
    // Initialize Calendar
    function initializeCalendar() {
        const newCalendar = new tui.Calendar('#calendar', {
            defaultView: 'month',
            useFormPopup: false,
            useDetailPopup: false,
            isReadOnly: false,
            taskView: true,
            usageStatistics: false,
            calendars: [
                {
                    id: '1',
                    name: 'Events',
                    color: '#ffffff',
                    bgColor: '#007bff',
                    borderColor: '#007bff'
                },
                {
                    id: '2',
                    name: 'Tasks',
                    color: '#ffffff',
                    bgColor: '#28a745',
                    borderColor: '#28a745'
                }
            ],
            template: {
                monthDayname: function (dayname) {
                    return `<span class="calendar-day">${dayname.label}</span>`;
                }
            }
        });


        // Calendar event handlers
        newCalendar.on('clickEvent', function (e) {
            const event = e.event;
                if (event.category == "task") {
                    // It's a task, show task details
                    //selectedTask = event;
                    showTaskDetails(event);
                } else {
                    // It's a normal event, show event details
                    //selectedEvent = event;
                    showEventDetails(event);
                }
        });

        newCalendar.on('beforeUpdateEvent', function (e) {
            updateEventOnServer(e.event, e.changes);
        });

        newCalendar.on('beforeDeleteEvent', function (e) {
            if (confirm('Are you sure you want to delete this event?')) {
                deleteEventFromServer(e.event.id);
            }
        });

        newCalendar.on('beforeDeleteEvent', function (e) {
            if (confirm('Are you sure you want to delete this task?')) {
                deleteTaskFromServer(e.event.id);
            }
        });

        newCalendar.on('beforeCreateEvent', function (e) {
            createQuickEvent(e);
        });

        /*calendar.on('dblclickDayname', function (e) {
            const date = e.date.toDate();
            openTaskForm({
                date: date.toISOString().split('T')[0]
            });
        });*/


        return newCalendar; 
    }

    function toggleCategory(categoryId, isVisible) {
        const schedules = calendar.getSchedules();
        schedules.forEach(schedule => {
            if (schedule.calendarId === categoryId) {
                if (isVisible) {
                    calendar.createEvents([schedule]);
                } else {
                    calendar.deleteEvent(schedule.id, schedule.calendarId);
                }
            }
        });
    }

    // Load events from server
    async function loadAllItems() {
        calendar.clear(); // ✅ Clear previous events if any

        try {
            const [events, tasks] = await Promise.all([
                fetch('/api/calendarapi/all').then(res => res.json()),
                fetch('/api/tasks/all').then(res => res.json()),
            ]);

            events.forEach(event => {
                calendar.createEvents([{
                    id: String(event.id),
                    calendarId: '1',
                    title: event.title,
                    category: event.isAllDay ? 'allday' : 'time',
                    start: isoStringToLocalDatetime(event.start + 'Z'),
                    end: isoStringToLocalDatetime(event.end + 'Z'),
                    isAllDay: event.isAllDay,
                    location: event.location,
                    backgroundColor: event.color || '#007bff',
                    borderColor: event.color || '#007bff',
                    raw: event
                }]);

            });

            tasks.forEach(task => {
                const taskDate = new Date(`${task.date}T00:00:00`);
                const importanceColorMap = {
                    Low: '#28a745',
                    Medium: '#ffc107',
                    High: '#dc3545'
                };

                const color = task.color || importanceColorMap[task.importance] || '#28a745';

                calendar.createEvents([{
                    id: String(task.id),
                    calendarId: '2',
                    title: task.isCompleted ? `✅ ${task.title}` : task.title,
                    category: 'task',
                    start: taskDate,
                    end: taskDate,
                    isAllDay: true,
                    backgroundColor: color,
                    borderColor: color,
                    raw: task
                }]);
            });
        }
        catch (error) {
            console.error('Error loading calendar data:', error);
            showToast('Error while loading calendar data', 'error');
        }
    }



    // Modal handlers
    const eventModal = new bootstrap.Modal(document.getElementById('eventModal'));
    const eventDetailsModal = new bootstrap.Modal(document.getElementById('eventDetailsModal'));
    const taskModal = new bootstrap.Modal(document.getElementById('taskModal'));
    const taskDetailsModal = new bootstrap.Modal(document.getElementById('taskDetailsModal'));

    // Edit event from details modal
    document.getElementById('editEventBtn').addEventListener('click', function () {
        eventDetailsModal.hide();
        setTimeout(() => {
            openEventForm(selectedEvent);
        }, 300);
    });

    document.getElementById('editTaskBtn').addEventListener('click', function () {
        taskDetailsModal.hide();
        setTimeout(() => {
            openTaskForm(selectedTask);
        }, 300);
    });

    document.getElementById('addEventBtn').addEventListener('click', () => {
        openEventForm();
    });

    document.getElementById('addTaskBtn').addEventListener('click', () => {
        openTaskForm();
    });


    // Helper functions
    function openTaskForm(task = null) {
       
        document.getElementById('taskForm').reset();
        selectedTask = task;
        if (task) {
            document.getElementById('taskModalLabel').innerHTML = '<i class="fas fa-edit me-2"></i>Edit Task';
            document.getElementById('taskId').value = task.id;
            document.getElementById('taskTitle').value = task.title;
            document.getElementById('taskDescription').value = task.description || '';
            document.getElementById('taskDueDate').value = task.date ? task.date.split('T')[0] : '';
            document.getElementById('taskPriority').value = task.importance || 'Medium';
            document.getElementById('taskStatus').value = task.isCompleted ? 'true' : 'false';
            document.getElementById('taskColor').value = task.color || '#28a745';

            document.getElementById('deleteTaskBtn').style.display = 'block';
        } else {
            document.getElementById('taskModalLabel').innerHTML = '<i class="fas fa-plus-circle me-2"></i>Create New Task';
            document.getElementById('taskId').value = '';
            document.getElementById('deleteTaskBtn').style.display = 'none';

            document.getElementById('taskDueDate').value = new Date().toISOString().split('T')[0];
            document.getElementById('taskStatus').value = 'false';
            document.getElementById('taskPriority').value = 'Medium';
            document.getElementById('taskColor').value = '#28a745';
        }

        taskModal.show();
    }
    function openEventForm(event = null) {
        //selectedEvent = event;
        document.getElementById('eventForm').reset();
        selectedEvent = event;

        if (event) {
            document.getElementById('eventModalLabel').innerHTML = '<i class="fas fa-edit me-2"></i>Edit event';
            document.getElementById('eventId').value = event.id;
            document.getElementById('eventTitle').value = event.title;
            document.getElementById('eventDescription').value = event.description || '';
            document.getElementById('eventLocation').value = event.location || '';
            document.getElementById('eventMeetingLink').value = event.meetingLink || '';
            document.getElementById('eventAllDay').checked = event.isAllDay;
            document.getElementById('eventIsInPerson').checked = event.isInPerson;
            document.getElementById('eventColor').value = event.color || '#007bff';

            // Set datetime values
            const start = new Date(event.start);
            const end = new Date(event.end);

            if (event.isAllDay) {
                document.getElementById('eventStart').type = 'date';
                document.getElementById('eventEnd').type = 'date';
                document.getElementById('eventStart').value = start.toISOString().split('T')[0];
                document.getElementById('eventEnd').value = end.toISOString().split('T')[0];
            } else {
                document.getElementById('eventStart').type = 'datetime-local';
                document.getElementById('eventEnd').type = 'datetime-local';
                document.getElementById('eventStart').value = formatDateTimeLocal(start);
                document.getElementById('eventEnd').value = formatDateTimeLocal(end);
            }

            // Show delete button
            document.getElementById('deleteEventBtn').style.display = 'block';
        } else {
            document.getElementById('eventModalLabel').innerHTML = '<i class="fas fa-plus-circle me-2"></i>Create new';
            document.getElementById('eventId').value = '';
            document.getElementById('deleteEventBtn').style.display = 'none';

            // Set default dates
            const now = new Date();
            const endTime = new Date(now.getTime() + 60 * 60 * 1000); // 1 hour later

            document.getElementById('eventStart').value = formatDateTimeLocal(now);
            document.getElementById('eventEnd').value = formatDateTimeLocal(endTime);
        }

        eventModal.show();
    }

    // Color picker
    document.querySelectorAll('.color-option').forEach(option => {
        option.addEventListener('click', function () {
            const color = this.dataset.color;
            document.getElementById('eventColor').value = color;

            // Update visual selection
            document.querySelectorAll('.color-option').forEach(o => o.classList.remove('selected'));
            this.classList.add('selected');
        });
    });

    // All-day checkbox handler
    document.getElementById('eventAllDay').addEventListener('change', function () {
        const startInput = document.getElementById('eventStart');
        const endInput = document.getElementById('eventEnd');

        if (this.checked) {
            // Convert to date inputs
            const startDate = startInput.value.split('T')[0];
            const endDate = endInput.value.split('T')[0];

            startInput.type = 'date';
            endInput.type = 'date';
            startInput.value = startDate;
            endInput.value = endDate;
        } else {
            // Convert back to datetime inputs
            startInput.type = 'datetime-local';
            endInput.type = 'datetime-local';
        }
    });

    // Form submission
    document.getElementById('eventForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        await saveEvent();
    });

    document.getElementById('taskForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        await saveTask();
    });


    async function saveEvent() {
        const eventIdValue = document.getElementById('eventId').value;
        const isNew = !eventIdValue || eventIdValue === '0';
        const isAllDay = document.getElementById('eventAllDay').checked;

        const event = {
            Id: isNew ? 0 : parseInt(eventIdValue, 10),
            Title: document.getElementById('eventTitle').value,
            Description: document.getElementById('eventDescription').value,
            Start: isAllDay
                ? new Date(document.getElementById('eventStart').value + 'T00:00:00').toISOString()
                : new Date(document.getElementById('eventStart').value).toISOString(),
            End: isAllDay
                ? new Date(document.getElementById('eventEnd').value + 'T23:59:59').toISOString()
                : new Date(document.getElementById('eventEnd').value).toISOString(),
            IsAllDay: isAllDay,
            Location: document.getElementById('eventLocation').value,
            MeetingLink: document.getElementById('eventMeetingLink').value,
            IsInPerson: document.getElementById('eventIsInPerson').checked,
            Color: document.getElementById('eventColor').value
        };

        const endpoint = isNew ? '/api/calendarapi/add' : '/api/calendarapi/replace';
        const method = isNew ? 'POST' : 'PUT';

        try {
            const response = await fetch(endpoint, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(event)
            });

            if (response.ok) {
                const result = isNew ? await response.json() : event.Id;

                if (isNew) {
                    calendar.createEvents([{
                        id: String(result),
                        calendarId: '1',
                        title: event.Title,
                        category: event.IsAllDay ? 'allday' : 'time',
                        start: event.Start,
                        end: event.End,
                        isAllDay: event.IsAllDay,
                        location: event.Location,
                        backgroundColor: event.Color,
                        borderColor: event.Color,
                        raw: event
                    }]);
                } else {
                    calendar.updateEvent(String(event.Id), '1', {
                        title: event.Title,
                        start: event.Start,
                        end: event.End,
                        isAllDay: event.IsAllDay,
                        location: event.Location,
                        backgroundColor: event.Color,
                        borderColor: event.Color,
                        raw: event
                    });
                }

                eventModal.hide();
                showToast(isNew ? 'Събитието е създадено успешно!' : 'Събитието е обновено успешно!', 'success');
            } else {
                const errorText = await response.text();
                showToast(`Грешка при запазване: ${errorText}`, 'error');
            }
        } catch (error) {
            console.error('Error saving event:', error);
            showToast('Грешка при запазване на събитието', 'error');
        }
    }

    async function saveTask() {
        const taskIdValue = document.getElementById('taskId').value;
        const isNew = !taskIdValue || taskIdValue === '0';

        const task = {
            id: isNew ? 0 : parseInt(taskIdValue, 10),
            title: document.getElementById('taskTitle').value,
            description: document.getElementById('taskDescription').value,
            //category: "task",
            date: document.getElementById('taskDueDate').value, // matches UserTask.Date
            importance: document.getElementById('taskPriority').value, // Low/Medium/High
            isCompleted: document.getElementById('taskStatus').value === 'true',
            color: document.getElementById('taskColor').value
        };

        const endpoint = isNew ? '/api/tasks/add' : '/api/tasks/replace';
        const method = isNew ? 'POST' : 'PUT';

        try {
            const response = await fetch(endpoint, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(task)
            });

            if (response.ok) {
                const result = isNew ? await response.json() : task.id;

                const start = new Date(`${task.date}T00:00:00`);
                const end = new Date(`${task.date}T23:59:59`);

                if (isNew) {
                    calendar.createEvents([{
                        id: String(result),
                        calendarId: '2',
                        title: task.title,
                        category: 'task',
                        start,
                        end,
                        isAllDay: true,
                        backgroundColor: task.color,
                        borderColor: task.color,
                        raw: task
                    }]);
                } else {
                    calendar.updateEvent(String(result), '2', {
                        title: task.title,
                        description: task.description,
                        //category: 'task',
                        start,
                        end,
                        isAllDay: true,
                        backgroundColor: task.color,
                        borderColor: task.color,
                        raw: task
                    });
                }

                taskModal.hide();
                showToast(isNew ? 'Task created successfully!' : 'Task updated successfully!', 'success');
            } else {
                const errorText = await response.text();
                showToast(`Error saving task: ${errorText}`, 'error');
            }
        } catch (error) {
            console.error('Error saving task:', error);
            showToast('Error saving task', 'error');
        }
    }
    function showEventDetails(event) {
        selectedEvent = {
            id: event.id,
            title: event.title,
            description: event.raw.description || '',
            start: event.start,
            end: event.end,
            isAllDay: event.isAllDay,
            location: event.location || '',
            meetingLink: event.raw.meetingLink || '',
            isInPerson: event.raw.isInPerson || false,
            color: event.backgroundColor || '#007bff'
        };

        const content = document.getElementById('eventDetailsContent');

        content.innerHTML = `
                    <div class="event-details">
                        <div class="event-detail-item">
                            <div class="event-color-indicator" style="background-color: ${event.backgroundColor}"></div>
                            <strong>${event.title}</strong>
                        </div>
                        ${event.raw.description ? `
                            <div class="event-detail-item">
                                <i class="fas fa-align-left event-detail-icon"></i>
                                <span>${event.raw.description}</span>
                            </div>
                        ` : ''}
                        <div class="event-detail-item">
                            <i class="fas fa-clock event-detail-icon"></i>
                            <span>${formatEventTime(event)}</span>
                        </div>
                        ${event.location ? `
                            <div class="event-detail-item">
                                <i class="fas fa-map-marker-alt event-detail-icon"></i>
                                <span>${event.location}</span>
                            </div>
                        ` : ''}
                        ${event.raw.meetingLink ? `
                            <div class="event-detail-item">
                                <i class="fas fa-link event-detail-icon"></i>
                                <a href="${event.raw.meetingLink}" target="_blank">${event.raw.meetingLink}</a>
                            </div>
                        ` : ''}
                        ${event.raw.isInPerson ? `
                            <div class="event-detail-item">
                                <i class="fas fa-users event-detail-icon"></i>
                                <span>In-person event</span>
                            </div>
                        ` : ''}
                    </div>
                `;

        eventDetailsModal.show();
    }

    function showTaskDetails(event) {
        const task = event.raw || event; 
        selectedTask = {
            id: event.id,
            title: task.title,
            description: task.description || '',
            date: task.date || event.start?.toISOString().split('T')[0],
            importance: task.importance || 'Medium',
            isCompleted: task.isCompleted || false,
            color: task.color || '#28a745'
        };

        const content = document.getElementById('taskDetailsContent');

        content.innerHTML = `
  <div class="event-details">
            <div class="event-detail-item">
                <div class="event-color-indicator" style="background-color: ${selectedTask.color};"></div>
                <strong>${selectedTask.title}</strong>
            </div>

            ${selectedTask.description ? `
                <div class="event-detail-item">
                    <i class="fas fa-align-left event-detail-icon"></i>
                    <span>${selectedTask.description}</span>
                </div>
            ` : ''}

            <div class="event-detail-item">
                <i class="fas fa-calendar-day event-detail-icon"></i>
                <span>Due: ${selectedTask.date}</span>
            </div>

            <div class="event-detail-item">
                <i class="fas fa-exclamation-circle event-detail-icon"></i>
                <span>Priority: ${selectedTask.importance}</span>
            </div>

            <div class="event-detail-item">
                <i class="fas fa-tasks event-detail-icon"></i>
                <span>Status: ${selectedTask.isCompleted ? '✅ Completed' : '❌ Incomplete'}</span>
            </div>
        </div>
                `;

        taskDetailsModal.show();
    }


    async function updateEventOnServer(event, changes) {
        try {
            const updatePayload = {
                id: parseInt(event.id),
                start: changes.start ? toLocalISOString(new Date(changes.start)) : toLocalISOString(new Date(changes.start)),
                end: changes.end ? toLocalISOString(new Date(changes.end)) : toLocalISOString(new Date(changes.end))
            };

            const response = await fetch('/api/calendarapi/move', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updatePayload)
            });

            if (response.ok) {
                calendar.updateEvent(event.id, event.calendarId, {
                    start: updatePayload.start,
                    end: updatePayload.end
                });
                showToast('Event was moved successfully!', 'success');
            } else {
                const errorText = await response.text();
                showToast(`Error while moving event: ${errorText}`, 'error');
                calendar.render(); // връщане назад ако не успее
            }
        } catch (error) {
            console.error('Error updating event:', error);
            showToast('Error while moving event', 'error');
            calendar.render();
        }
    }

   

    // Delete event
    document.getElementById('deleteEventBtn').addEventListener('click', async function () {
        if (selectedEvent && confirm('Are you sure you want to delete this event?')) {
            await deleteEventFromServer(selectedEvent.id);
        }
    });

    document.getElementById('deleteTaskBtn').addEventListener('click', async function (e) {
        if (selectedTask && confirm('Are you sure you want to delete this task?')) {
            await deleteTaskFromServer(selectedTask.id);
        }
    });

    async function deleteEventFromServer(eventId) {
        try {
            const response = await fetch(`/api/calendarapi/delete/${eventId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                calendar.deleteEvent(eventId, '1');
                eventModal.hide();
                eventDetailsModal.hide();
                showToast('Event was deleted successfully!', 'success');


                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                const errorText = await response.text();
                showToast(`Error while deleting event: ${errorText}`, 'error');
            }
        } catch (error) {
            console.error('Error deleting event:', error);
            showToast('Error while deleting event', 'error');
        }
    }

    async function deleteTaskFromServer(eventId) {
        try {
            const response = await fetch(`/api/tasks/delete/${eventId}`, {
                method: 'DELETE',
            });

            if (response.ok) {
                calendar.deleteEvent(eventId, '2');
                eventModal.hide();
                eventDetailsModal.hide();
                showToast('Task was deleted successfully!', 'success');


                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                const errorText = await response.text();
                showToast(`Error while deleting task: ${errorText}`, 'error');
            }
        } catch (error) {
            console.error('Error deleting task:', error);
            showToast('Error while deleting task', 'error');
        }
    }


    function createQuickEvent(eventData) {
        const title = prompt('Insert event title:');
        if (title) {
            const quickEvent = {
                Id: 0,
                Title: title,
                Description: '',
                Start: eventData.start.toISOString(),
                End: eventData.end.toISOString(),
                IsAllDay: eventData.isAllDay || false,
                Location: '',
                MeetingLink: '',
                IsInPerson: false,
                Color: '#007bff'
            };

            // Save to server
            fetch('/api/calendarapi/add', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(quickEvent)
            })
                .then(response => response.json())
                .then(eventId => {
                    calendar.createEvents([{
                        id: String(eventId),
                        calendarId: '1',
                        title: quickEvent.Title,
                        category: quickEvent.IsAllDay ? 'allday' : 'time',
                        start: quickEvent.Start,
                        end: quickEvent.End,
                        isAllDay: quickEvent.IsAllDay,
                        backgroundColor: quickEvent.Color,
                        borderColor: quickEvent.Color,
                        raw: quickEvent
                    }]);
                    showToast('Event was created successfully!', 'success');
                })
                .catch(error => {
                    console.error('Error creating quick event:', error);
                    showToast('Error while creating event', 'error');
                });
        }
    }

    function showToast(message, type = 'success') {
        const toastContainer = document.querySelector('.toast-container');
        const toastId = 'toast-' + Date.now();

        const toast = document.createElement('div');
        toast.className = `toast toast-${type} show`;
        toast.id = toastId;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
                    <div class="toast-header">
                        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-triangle'} me-2"></i>
                        <strong class="me-auto">${type === 'success' ? 'Success' : 'Error'}</strong>
                        <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
                    </div>
                    <div class="toast-body">${message}</div>
                `;

        toastContainer.appendChild(toast);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            const toastElement = document.getElementById(toastId);
            if (toastElement) {
                toastElement.remove();
            }
        }, 5000);
    }

    function toISO(dateValue) {
        try {
            return new Date(dateValue).toISOString();
        } catch {
            console.warn("Invalid date:", dateValue);
            return null;
        }
    }

    function formatEventTime(event) {
        const start = new Date(event.start);
        const end = new Date(event.end);

        if (event.isAllDay) {
            return `${start.toLocaleDateString('bg-BG')} - All day`;
        } else {
            return `${start.toLocaleDateString('bg-BG')} ${start.toLocaleTimeString('bg-BG', { hour: '2-digit', minute: '2-digit' })} - ${end.toLocaleTimeString('bg-BG', { hour: '2-digit', minute: '2-digit' })}`;
        }
    }

    function formatDateTimeLocal(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day}T${hours}:${minutes}`;
    }

    function localDatetimeToISOString(localDateTime) {
        const dt = new Date(localDateTime);
        return toLocalISOString(dt);
    }
    function isoStringToLocalDatetime(isoString) {
        const dt = new Date(isoString);
        // Get the local datetime components
        const year = dt.getFullYear();
        const month = String(dt.getMonth() + 1).padStart(2, '0');
        const day = String(dt.getDate()).padStart(2, '0');
        const hours = String(dt.getHours()).padStart(2, '0');
        const minutes = String(dt.getMinutes()).padStart(2, '0');
        const seconds = String(dt.getSeconds()).padStart(2, '0');

        // Format as local datetime string similar to input format: "YYYY-MM-DDTHH:mm:ss"
        return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
    }
    function toLocalISOString(date) {
        const pad = (n) => n.toString().padStart(2, '0')
        const year = date.getFullYear()
        const month = pad(date.getMonth() + 1)
        const day = pad(date.getDate())
        const hours = pad(date.getHours())
        const minutes = pad(date.getMinutes())
        const seconds = pad(date.getSeconds())
        const offset = -date.getTimezoneOffset()
        const sign = offset >= 0 ? '+' : '-'
        const offsetHours = pad(Math.floor(Math.abs(offset) / 60))
        const offsetMinutes = pad(Math.abs(offset) % 60)
        return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}${sign}${offsetHours}:${offsetMinutes}`
    }

    // Keyboard shortcuts
    document.addEventListener('keydown', function (e) {
        if (e.ctrlKey || e.metaKey) {
            switch (e.key) {
                case 'n':
                    e.preventDefault();
                    openEventForm();
                    break;
                case 'ArrowLeft':
                    e.preventDefault();
                    calendar.prev();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    calendar.next();
                    break;
                case 't':
                    e.preventDefault();
                    calendar.today();
                    break;
            }
        }
    });

    // Auto-refresh events periodically
    setInterval(function () {
        if (!document.hidden) {
            // Refresh events every 5 minutes if page is visible
            loadEvents();
        }
    }, 300000); // 5 minutes

    // Handle window resize
    window.addEventListener('resize', function () {
        if (calendar) {
            calendar.render();
        }
    });
});