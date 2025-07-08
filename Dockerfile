# ---- build/development stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev

# install OpenSSH (optional)
RUN apt-get update \
 && apt-get install -y --no-install-recommends openssh-server \
 && useradd -ms /bin/bash dev \
 && echo "dev:dev" | chpasswd \
 && mkdir /var/run/sshd

WORKDIR /workspace

EXPOSE 22

CMD ["/usr/sbin/sshd","-D"]

